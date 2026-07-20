using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class ListarPedidosDeCompraPaginadoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly ListarPedidosDeCompraPaginadoUseCase _useCase;

        public ListarPedidosDeCompraPaginadoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new ListarPedidosDeCompraPaginadoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoNumeroPaginaForInvalido()
        {
            var entrada = new ListarPedidosDeCompraPaginadoEntrada(NumeroPagina: 0, TamanhoPagina: 10);

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");

            await _pedidosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarPedidosDeCompraFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPeriodoForInvalido()
        {
            var entrada = new ListarPedidosDeCompraPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 10,
                DataInicio: new DateTime(2026, 2, 10),
                DataFim: new DateTime(2026, 2, 1));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PERIODO_INVALIDO");
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarPaginaMapeada_QuandoParametrosForemValidos()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            var pagina = new ResultadoPaginado<PedidoDeCompra>(
                Itens: new List<PedidoDeCompra> { pedido },
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 1);

            _pedidosRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarPedidosDeCompraFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<PedidoDeCompra>>.Sucesso(pagina));

            var entrada = new ListarPedidosDeCompraPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 10,
                Status: StatusPedidoDeCompra.Aprovada);

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.TotalRegistros.Should().Be(1);
            resultado.Instancia.TotalPaginas.Should().Be(1);
            resultado.Instancia.Itens.Should().ContainSingle();
            resultado.Instancia.Itens.First().Status.Should().Be("Aprovada");
            resultado.Instancia.Itens.First().ValorTotal.Should().Be(50.00m);
        }
    }
}
