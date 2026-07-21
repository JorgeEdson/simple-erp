using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Handlers;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Producao.Eventos;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ManipuladorMovimentacoesPorProducaoTests
    {
        private const long IdOrdem = 202604020400;
        private const long IdProdutoFabricado = 202604020050;
        private const long IdInsumoA = 202604020010;
        private const long IdInsumoB = 202604020011;

        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrar =
            Substitute.For<IRegistrarMovimentacaoDeEstoqueUseCase>();
        private readonly ILogService _logService = Substitute.For<ILogService>();
        private readonly MovimentacoesPorProducaoHandler _handler;

        public ManipuladorMovimentacoesPorProducaoTests()
        {
            _handler = new MovimentacoesPorProducaoHandler(_registrar, _logService);
        }

        private static OrdemDeProducaoConcluida EventoComDoisInsumos() =>
            new(
                Id.TentarCriar(IdOrdem).Instancia,
                Id.TentarCriar(IdProdutoFabricado).Instancia,
                quantidadeProduzida: 5m,
                insumosConsumidos: new List<InsumoConsumido>
                {
                    new(IdInsumoA, 10m),
                    new(IdInsumoB, 15m)
                });

        private void ConfigurarSucesso() =>
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                    new RegistrarMovimentacaoDeEstoqueSaida(1, 1, "EntradaPorProducao", "Entrada", 1m, 1m)));

        [Fact]
        public async Task ManipularAsync_DeveGerarSaidasDosInsumosEEntradaDoAcabado_ComOrigemNaOrdem()
        {
            ConfigurarSucesso();

            var resultado = await _handler.ManipularAsync(EventoComDoisInsumos());

            resultado.EhSucesso.Should().BeTrue();

            // 2 saídas de matéria-prima + 1 entrada do produto acabado
            await _registrar
                .Received(3)
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>());

            await _registrar
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<RegistrarMovimentacaoDeEstoqueEntrada>(e =>
                        e.IdProduto == IdInsumoA &&
                        e.Quantidade == 10m &&
                        e.Tipo == TipoDeMovimentacao.SaidaPorProducao &&
                        e.OrigemTipo == TipoOrigemMovimentacao.Producao &&
                        e.OrigemIdReferencia == IdOrdem),
                    Arg.Any<CancellationToken>());

            await _registrar
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<RegistrarMovimentacaoDeEstoqueEntrada>(e =>
                        e.IdProduto == IdProdutoFabricado &&
                        e.Quantidade == 5m &&
                        e.Tipo == TipoDeMovimentacao.EntradaPorProducao &&
                        e.OrigemIdReferencia == IdOrdem),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_DeveRetornarFalha_QuandoUmaMovimentacaoFalhar()
        {
            _registrar
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha("SALDO_INSUFICIENTE"));

            var resultado = await _handler.ManipularAsync(EventoComDoisInsumos());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SALDO_INSUFICIENTE");
        }
    }
}
