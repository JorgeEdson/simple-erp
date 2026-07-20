using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ConsultarExtratoDeMovimentacoesPaginadoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMovimentacaoDeEstoqueRepository _movimentacoesRepository;
        private readonly ILogService _logService;
        private readonly ConsultarExtratoDeMovimentacoesPaginadoUseCase _useCase;

        public ConsultarExtratoDeMovimentacoesPaginadoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _movimentacoesRepository = Substitute.For<IMovimentacaoDeEstoqueRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.MovimentacoesDeEstoqueRepository.Returns(_movimentacoesRepository);

            _useCase = new ConsultarExtratoDeMovimentacoesPaginadoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPeriodoForInvalido()
        {
            var entrada = new ConsultarExtratoDeMovimentacoesPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 10,
                DataInicio: new DateTime(2026, 3, 10),
                DataFim: new DateTime(2026, 3, 1));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PERIODO_INVALIDO");

            await _movimentacoesRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarMovimentacoesDeEstoqueFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarExtratoMapeado_QuandoParametrosForemValidos()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;
            var origem = OrigemDaMovimentacao.TentarCriar(TipoOrigemMovimentacao.Compra, 500).Instancia;
            var quantidade = Quantidade.TentarCriar(5m).Instancia;

            var movimentacao = MovimentacaoDeEstoque.Criar(
                idProduto,
                TipoDeMovimentacao.EntradaPorCompra,
                quantidade,
                saldoResultante: 5m,
                origem).Instancia;

            var pagina = new ResultadoPaginado<MovimentacaoDeEstoque>(
                Itens: new List<MovimentacaoDeEstoque> { movimentacao },
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 1);

            _movimentacoesRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarMovimentacoesDeEstoqueFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<MovimentacaoDeEstoque>>.Sucesso(pagina));

            var entrada = new ConsultarExtratoDeMovimentacoesPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 10,
                IdProduto: 202604020001,
                Tipo: TipoDeMovimentacao.EntradaPorCompra);

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.TotalRegistros.Should().Be(1);
            resultado.Instancia.Itens.Should().ContainSingle();

            var item = resultado.Instancia.Itens.First();
            item.IdProduto.Should().Be(202604020001);
            item.Tipo.Should().Be("EntradaPorCompra");
            item.Sentido.Should().Be("Entrada");
            item.SaldoResultante.Should().Be(5m);
            item.OrigemTipo.Should().Be("Compra");
            item.OrigemIdReferencia.Should().Be(500);
        }
    }
}
