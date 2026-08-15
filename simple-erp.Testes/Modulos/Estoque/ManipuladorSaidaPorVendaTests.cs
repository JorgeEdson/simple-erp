using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Handlers;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Vendas.Eventos;
using System.Collections.Generic;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ManipuladorSaidaPorVendaTests
    {
        private static readonly Guid IdPedido = new Guid("00000000-0000-0000-0000-202604020500");
        private static readonly Guid IdCliente = new Guid("00000000-0000-0000-0000-202604020001");

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
                IdPedido,
                IdCliente,
                valorTotal: 100.00m,
                itens: new List<ItemVendaAprovado>
                {
                    new(new Guid("00000000-0000-0000-0000-202604020010"), 2m),
                    new(new Guid("00000000-0000-0000-0000-202604020011"), 3m)
                });

        private void ConfigurarSucesso() =>
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                    new RegistrarMovimentacaoDeEstoqueSaida(new Guid("00000000-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000001"), "SaidaPorVenda", "Saida", 1m, 1m)));

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
                        e.IdProduto == new Guid("00000000-0000-0000-0000-202604020010") &&
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
