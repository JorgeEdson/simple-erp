using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Handlers;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ManipuladorEntradaPorCompraTests
    {
        private const long IdPedido = 202604020003;
        private const long IdFornecedor = 202604020002;

        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrar =
            Substitute.For<IRegistrarMovimentacaoDeEstoqueUseCase>();
        private readonly ILogService _logService = Substitute.For<ILogService>();
        private readonly ManipuladorEntradaPorCompra _handler;

        public ManipuladorEntradaPorCompraTests()
        {
            _handler = new ManipuladorEntradaPorCompra(_registrar, _logService);
        }

        private static PedidoDeCompraEfetivado EventoComDoisItens() =>
            new(
                Id.TentarCriar(IdPedido).Instancia,
                Id.TentarCriar(IdFornecedor).Instancia,
                valorTotal: 22.00m,
                itens: new List<ItemPedidoDeCompraEfetivado>
                {
                    new(202604020010, 5m, 2.00m),
                    new(202604020011, 3m, 4.00m)
                });

        private void ConfigurarSucesso() =>
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                    new RegistrarMovimentacaoDeEstoqueSaida(1, 1, "EntradaPorCompra", "Entrada", 1m, 1m)));

        [Fact]
        public async Task ManipularAsync_DeveRegistrarUmaEntradaPorItem_ComOrigemNoPedido()
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
                        e.Quantidade == 5m &&
                        e.Tipo == TipoDeMovimentacao.EntradaPorCompra &&
                        e.OrigemTipo == TipoOrigemMovimentacao.Compra &&
                        e.OrigemIdReferencia == IdPedido),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_DeveRetornarFalha_QuandoUmaMovimentacaoFalhar()
        {
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha("PRODUTO_NAO_ENCONTRADO"));

            var resultado = await _handler.ManipularAsync(EventoComDoisItens());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");
        }
    }
}
