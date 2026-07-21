using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;


namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class ListarFornecedoresPaginadoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly ListarFornecedoresPaginadoUseCase _useCase;

        public ListarFornecedoresPaginadoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ListarFornecedoresPaginadoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoNumeroPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 10);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");

            await _fornecedoresRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _fornecedoresRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaExcederOLimiteMaximo()
        {
            // Arrange
            var entrada = new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 101);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            await _fornecedoresRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoHouverMultiplosErrosDeValidacao()
        {
            // Arrange
            var entrada = new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _fornecedoresRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoRepositorioFalharAoListar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Fornecedor>>.Falha("ERRO_AO_LISTAR_FORNECEDORES"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_LISTAR_FORNECEDORES");

            await _fornecedoresRepository
                .Received(1)
                .ListarPaginadoAsync(
                    entrada.NumeroPagina,
                    entrada.TamanhoPagina,
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveListarComSucesso_QuandoNaoHouverItens()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            var pagina = new ResultadoPaginado<Fornecedor>(
                Itens: new List<Fornecedor>(),
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 0);

            _fornecedoresRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Fornecedor>>.Sucesso(pagina));

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

            var fornecedor1 = FornecedorBuilder.Novo()
                .ComId(1001)
                .Criar();

            var fornecedor2 = FornecedorBuilder.Novo()
                .ComId(1002)
                .Criar();

            var pagina = new ResultadoPaginado<Fornecedor>(
                Itens: new List<Fornecedor> { fornecedor1, fornecedor2 },
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 2);

            _fornecedoresRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Fornecedor>>.Sucesso(pagina));

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
            item1.Id.Should().Be(fornecedor1.Id.Valor);
            item1.Nome.Should().Be(fornecedor1.Nome.Valor);
            item1.Documento.Should().Be(fornecedor1.Documento.Valor);
            item1.Email.Should().Be(fornecedor1.Email.Valor);
            item1.Ativo.Should().Be(fornecedor1.Ativo);
            item1.Cidade.Should().Be(fornecedor1.Endereco.Cidade);
            item1.Estado.Should().Be(fornecedor1.Endereco.Estado);

            var item2 = resultado.Instancia.Itens.ElementAt(1);
            item2.Id.Should().Be(fornecedor2.Id.Valor);
            item2.Nome.Should().Be(fornecedor2.Nome.Valor);
            item2.Documento.Should().Be(fornecedor2.Documento.Valor);
            item2.Email.Should().Be(fornecedor2.Email.Valor);
            item2.Ativo.Should().Be(fornecedor2.Ativo);
            item2.Cidade.Should().Be(fornecedor2.Endereco.Cidade);
            item2.Estado.Should().Be(fornecedor2.Endereco.Estado);
        }

        [Fact]
        public async Task ExecutarAsync_DeveEnviarFiltrosCorretamenteParaORepositorio()
        {
            // Arrange
            var entrada = new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: 2,
                TamanhoPagina: 20,
                Nome: "Fornecedor XPTO",
                Documento: "12345678909",
                Email: "fornecedor@teste.com",
                Ativo: true,
                Cidade: "Fortaleza",
                Estado: "CE");

            var pagina = new ResultadoPaginado<Fornecedor>(
                Itens: new List<Fornecedor>(),
                NumeroPagina: 2,
                TamanhoPagina: 20,
                TotalRegistros: 0);

            _fornecedoresRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarFornecedoresFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Fornecedor>>.Sucesso(pagina));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();

            await _fornecedoresRepository
                .Received(1)
                .ListarPaginadoAsync(
                    2,
                    20,
                    Arg.Is<ListarFornecedoresFiltros>(f =>
                        f.Nome == "Fornecedor XPTO" &&
                        f.Documento == "12345678909" &&
                        f.Email == "fornecedor@teste.com" &&
                        f.Ativo == true &&
                        f.Cidade == "Fortaleza" &&
                        f.Estado == "CE"),
                    Arg.Any<CancellationToken>());
        }

        private static ListarFornecedoresPaginadoEntrada CriarEntradaValida(
            int numeroPagina = 1,
            int tamanhoPagina = 10)
        {
            return new ListarFornecedoresPaginadoEntrada(
                NumeroPagina: numeroPagina,
                TamanhoPagina: tamanhoPagina);
        }
    }
}