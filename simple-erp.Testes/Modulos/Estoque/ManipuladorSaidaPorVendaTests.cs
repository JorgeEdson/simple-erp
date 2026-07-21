using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Handlers;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Vendas.Eventos;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ManipuladorSaidaPorVendaTests
    {
        private const long IdPedido = 202604020500;
        private const long IdCliente = 202604020001;

        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrar =
            Substitute.For<IRegistrarMovimentacaoDeEstoqueUseCase>();
        private readonly ILogService _logService = Substitute.For<ILogService>();
        private readonly SaidaPorVendaHandler _handler;

        public ManipuladorSaidaPorVendaTests()
        {
            _handler = new SaidaPorVendaHandler(_registrar, _logService);
        }

        private static PedidoDeVendaAprovado EventoComDoisItens() =>
            new(
                Id.TentarCriar(IdPedido).Instancia,
                Id.TentarCriar(IdCliente).Instancia,
                valorTotal: 100.00m,
                itens: new List<ItemVendaAprovado>
                {
                    new(202604020010, 2m),
                    new(202604020011, 3m)
                });

        private void ConfigurarSucesso() =>
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                    new RegistrarMovimentacaoDeEstoqueSaida(1, 1, "SaidaPorVenda", "Saida", 1m, 1m)));

        [Fact]
        public async Task ManipularAsync_DeveRegistrarUmaSaidaPorItem_ComOrigemNoPedido()
        {
            ConfigurarSucesso();

            var resultado = await _handler.ManipularAsync(EventoComDoisItens());

            resultado.EhSucesso.Should().BeTrue();

            await _registrar
                .Received(2)
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>());

            await _registrar
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<RegistrarMovimentacaoDeEstoqueEntrada>(e =>
                        e.IdProduto == 202604020010 &&
                        e.Quantidade == 2m &&
                        e.Tipo == TipoDeMovimentacao.SaidaPorVenda &&
                        e.OrigemTipo == TipoOrigemMovimentacao.Venda &&
                        e.OrigemIdReferencia == IdPedido),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_DeveRetornarFalha_QuandoUmaMovimentacaoFalhar()
        {
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha("SALDO_INSUFICIENTE"));

            var resultado = await _handler.ManipularAsync(EventoComDoisItens());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SALDO_INSUFICIENTE");
        }
    }
}
