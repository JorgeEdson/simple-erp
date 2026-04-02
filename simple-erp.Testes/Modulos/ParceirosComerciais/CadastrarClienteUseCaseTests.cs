using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;


namespace simple_erp.Testes.Modulos.ParceirosComerciais
{
    public sealed class CadastrarClienteUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClienteRepository _clientesRepository;
        private readonly CadastrarClienteUseCase _useCase;

        public CadastrarClienteUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _unitOfWork.ClientesRepository.Returns(_clientesRepository);
            _useCase = new CadastrarClienteUseCase(_unitOfWork);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new CadastrarClienteEntrada(
                Documento: "123",
                Nome: "",
                Email: "email-invalido",
                Rua: "",
                Numero: "",
                Complemento: "",
                Bairro: "",
                Cidade: "",
                Estado: "Ceara",
                Cep: "",
                Pais: "");

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _clientesRepository
                .DidNotReceive()
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>());

            await _clientesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoVerificarExistenciaPorDocumento()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_CONSULTAR_CLIENTE"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_CONSULTAR_CLIENTE");

            await _clientesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoJaExistirClienteComMesmoDocumento()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_JA_CADASTRADO");

            await _clientesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _clientesRepository
                .AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");

            await _clientesRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Nome.Valor == entrada.Nome &&
                        c.Email.Valor == entrada.Email &&
                        c.Ativo),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCadastrarClienteComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _clientesRepository
                .AdicionarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();

            resultado.Instancia.Should().NotBeNull();
            resultado.Instancia.Nome.Should().Be(entrada.Nome);
            resultado.Instancia.Email.Should().Be(entrada.Email);
            resultado.Instancia.Ativo.Should().BeTrue();
            resultado.Instancia.Id.Should().BeGreaterThan(0);

            await _clientesRepository
                .Received(1)
                .ExistePorDocumentoAsync(
                    Arg.Is<Documento>(d => d.Valor == entrada.Documento || d.Formatado == resultado.Instancia.Documento),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Nome.Valor == entrada.Nome &&
                        c.Email.Valor == entrada.Email &&
                        c.Ativo &&
                        c.Endereco.Rua == entrada.Rua &&
                        c.Endereco.Numero == entrada.Numero &&
                        c.Endereco.Complemento == entrada.Complemento &&
                        c.Endereco.Bairro == entrada.Bairro &&
                        c.Endereco.Cidade == entrada.Cidade &&
                        c.Endereco.Estado == entrada.Estado &&
                        c.Endereco.Cep == entrada.Cep &&
                        c.Endereco.Pais == entrada.Pais),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        private static CadastrarClienteEntrada CriarEntradaValida()
        {
            return new CadastrarClienteEntrada(
                Documento: "12345678909",
                Nome: "Cliente Teste",
                Email: "cliente@teste.com",
                Rua: "Rua das Flores",
                Numero: "123",
                Complemento: "Apto 101",
                Bairro: "Centro",
                Cidade: "Fortaleza",
                Estado: "CE",
                Cep: "60000-000",
                Pais: "Brasil");
        }
    }
}
