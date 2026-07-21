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
    public sealed class ListarClientesPaginadoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClienteRepository _clientesRepository;
        private readonly ILogService _logService;
        private readonly ListarClientesPaginadoUseCase _useCase;

        public ListarClientesPaginadoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ClientesRepository.Returns(_clientesRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ListarClientesPaginadoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoNumeroPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarClientesPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 10);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");

            await _clientesRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaForInvalido()
        {
            // Arrange
            var entrada = new ListarClientesPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _clientesRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoTamanhoPaginaExcederOLimiteMaximo()
        {
            // Arrange
            var entrada = new ListarClientesPaginadoEntrada(
                NumeroPagina: 1,
                TamanhoPagina: 101);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            await _clientesRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoHouverMultiplosErrosDeValidacao()
        {
            // Arrange
            var entrada = new ListarClientesPaginadoEntrada(
                NumeroPagina: 0,
                TamanhoPagina: 0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NUMERO_PAGINA_INVALIDO");
            resultado.Erros.Should().Contain("TAMANHO_PAGINA_INVALIDO");

            await _clientesRepository
                .DidNotReceive()
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoRepositorioFalharAoListar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Cliente>>.Falha("ERRO_AO_LISTAR_CLIENTES"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_LISTAR_CLIENTES");

            await _clientesRepository
                .Received(1)
                .ListarPaginadoAsync(
                    entrada.NumeroPagina,
                    entrada.TamanhoPagina,
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveListarComSucesso_QuandoNaoHouverItens()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            var pagina = new ResultadoPaginado<Cliente>(
                Itens: new List<Cliente>(),
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 0);

            _clientesRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Cliente>>.Sucesso(pagina));

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

            var cliente1 = ClienteBuilder.Novo()
                .ComId(1001)
                .Criar();

            var cliente2 = ClienteBuilder.Novo()
                .ComId(1002)
                .Criar();

            var pagina = new ResultadoPaginado<Cliente>(
                Itens: new List<Cliente> { cliente1, cliente2 },
                NumeroPagina: 1,
                TamanhoPagina: 10,
                TotalRegistros: 2);

            _clientesRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Cliente>>.Sucesso(pagina));

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
            item1.Id.Should().Be(cliente1.Id.Valor);
            item1.Nome.Should().Be(cliente1.Nome.Valor);
            item1.Documento.Should().Be(cliente1.Documento.Valor);
            item1.Email.Should().Be(cliente1.Email.Valor);
            item1.Ativo.Should().Be(cliente1.Ativo);
            item1.Cidade.Should().Be(cliente1.Endereco.Cidade);
            item1.Estado.Should().Be(cliente1.Endereco.Estado);

            var item2 = resultado.Instancia.Itens.ElementAt(1);
            item2.Id.Should().Be(cliente2.Id.Valor);
            item2.Nome.Should().Be(cliente2.Nome.Valor);
            item2.Documento.Should().Be(cliente2.Documento.Valor);
            item2.Email.Should().Be(cliente2.Email.Valor);
            item2.Ativo.Should().Be(cliente2.Ativo);
            item2.Cidade.Should().Be(cliente2.Endereco.Cidade);
            item2.Estado.Should().Be(cliente2.Endereco.Estado);
        }

        [Fact]
        public async Task ExecutarAsync_DeveEnviarFiltrosCorretamenteParaORepositorio()
        {
            // Arrange
            var entrada = new ListarClientesPaginadoEntrada(
                NumeroPagina: 2,
                TamanhoPagina: 20,
                Nome: "Jorge",
                Documento: "12345678909",
                Email: "jorge@teste.com",
                Ativo: true,
                Cidade: "Fortaleza",
                Estado: "CE");

            var pagina = new ResultadoPaginado<Cliente>(
                Itens: new List<Cliente>(),
                NumeroPagina: 2,
                TamanhoPagina: 20,
                TotalRegistros: 0);

            _clientesRepository
                .ListarPaginadoAsync(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<ListarClientesFiltros?>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<ResultadoPaginado<Cliente>>.Sucesso(pagina));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();

            await _clientesRepository
                .Received(1)
                .ListarPaginadoAsync(
                    2,
                    20,
                    Arg.Is<ListarClientesFiltros>(f =>
                        f.Nome == "Jorge" &&
                        f.Documento == "12345678909" &&
                        f.Email == "jorge@teste.com" &&
                        f.Ativo == true &&
                        f.Cidade == "Fortaleza" &&
                        f.Estado == "CE"),
                    Arg.Any<CancellationToken>());
        }

        private static ListarClientesPaginadoEntrada CriarEntradaValida(
            int numeroPagina = 1,
            int tamanhoPagina = 10)
        {
            return new ListarClientesPaginadoEntrada(
                NumeroPagina: numeroPagina,
                TamanhoPagina: tamanhoPagina);
        }
    }
}
