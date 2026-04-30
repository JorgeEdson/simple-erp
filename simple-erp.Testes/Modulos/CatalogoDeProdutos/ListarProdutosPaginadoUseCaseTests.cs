using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos
{
    public sealed class ListarProdutosPaginadoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly ListarProdutosPaginadoUseCase _useCase;

        public ListarProdutosPaginadoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ListarProdutosPaginadoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoNumeroPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 10);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");

            await _produtosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _produtosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaExcederOLimiteMaximo()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 101);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            await _produtosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoClassificacaoFiltroForInvalida()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 10,
                Classificacao: "Imaginaria");

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLASSIFICACAO_PRODUTO_INVALIDA");

            await _produtosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoHouverMultiplosErrosDeValidacao()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _produtosRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoRepositorioFalharAoListar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Produto>>.Falha("ERRO_AO_LISTAR_PRODUTOS"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_LISTAR_PRODUTOS");

            await _produtosRepository
                .Received(1)
                .ListarPaginadoAsync(
                    entrada.NumeroPagina,
                    entrada.TamanhoPagina,
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveListarComSucesso_QuandoNaoHouverItens()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            var pagina = new ResultadoPaginado<Produto>(
                Itens: new List<Produto>(),
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 0);

            _produtosRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Produto>>.Sucesso(pagina));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.NumeroPagina.Should().Be(1);
            resultado.Instancia.TamanhoPagina.Should().Be(10);
            resultado.Instancia.TotalRegistros.Should().Be(0);
            resultado.Instancia.TotalPaginas.Should().Be(0);
            resultado.Instancia.Itens.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutarAsync_DeveListarComSucesso_QuandoHouverItens()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            var produto1 = ProdutoBuilder.Novo()
                .ComId(1001)
                .ComCodigo("PROD-001")
                .ComoFabricado()
                .Criar();

            var produto2 = ProdutoBuilder.Novo()
                .ComId(1002)
                .ComCodigo("PROD-002")
                .ComoMateriaPrima()
                .Criar();

            var pagina = new ResultadoPaginado<Produto>(
                Itens: new List<Produto> { produto1, produto2 },
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 2);

            _produtosRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Produto>>.Sucesso(pagina));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.NumeroPagina.Should().Be(1);
            resultado.Instancia.TamanhoPagina.Should().Be(10);
            resultado.Instancia.TotalRegistros.Should().Be(2);
            resultado.Instancia.TotalPaginas.Should().Be(1);
            resultado.Instancia.Itens.Should().HaveCount(2);

            var item1 = resultado.Instancia.Itens.ElementAt(0);
            item1.Id.Should().Be(produto1.Id.Valor);
            item1.Codigo.Should().Be(produto1.Codigo.Valor);
            item1.Descricao.Should().Be(produto1.Descricao.Valor);
            item1.UnidadeDeMedida.Should().Be(produto1.UnidadeDeMedida.Valor);
            item1.Classificacao.Should().Be(produto1.Classificacao.ToString());
            item1.Ativo.Should().Be(produto1.Ativo);

            var item2 = resultado.Instancia.Itens.ElementAt(1);
            item2.Id.Should().Be(produto2.Id.Valor);
            item2.Codigo.Should().Be(produto2.Codigo.Valor);
            item2.Descricao.Should().Be(produto2.Descricao.Valor);
            item2.UnidadeDeMedida.Should().Be(produto2.UnidadeDeMedida.Valor);
            item2.Classificacao.Should().Be(produto2.Classificacao.ToString());
            item2.Ativo.Should().Be(produto2.Ativo);
        }

        [Fact]
        public async Task ExecutarAsync_DeveEnviarFiltrosCorretamenteParaORepositorio()
        {
            // Arrange
            var entrada = new ListarProdutosPaginadoEntrada(
                NumeroPagina: 2,
                TamanhoPagina: 20,
                Codigo: "PROD-001",
                Descricao: "Açúcar",
                UnidadeDeMedida: "KG",
                Classificacao: "Fabricado",
                Ativo: true);

            var pagina = new ResultadoPaginado<Produto>(
                Itens: new List<Produto>(),
                NumeroPagina: 2,
                TamanhoPagina: 20,
                TotalRegistros: 0);

            _produtosRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarProdutosFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Produto>>.Sucesso(pagina));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();

            await _produtosRepository
                .Received(1)
                .ListarPaginadoAsync(
                    2,
                    20,
                    Arg.Is<ListarProdutosFiltros>(f =>
                        f.Codigo == "PROD-001" &&
                        f.Descricao == "Açúcar" &&
                        f.UnidadeDeMedida == "KG" &&
                        f.Classificacao == ClassificacaoProduto.Fabricado &&
                        f.Ativo == true),
                    Arg.Any<CancellationToken>());
        }

        private static ListarProdutosPaginadoEntrada CriarEntradaValida(
            int numeroPagina = 1,
            int tamanhoPagina = 10)
        {
            return new ListarProdutosPaginadoEntrada(
                NumeroPagina: numeroPagina,
                TamanhoPagina: tamanhoPagina);
        }
    }
}
