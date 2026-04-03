using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais
{
    public sealed class ObterClientePorIdUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClienteRepository _clientesRepository;
        private readonly ILogService _logService;
        private readonly ObterClientePorIdUseCase _useCase;

        public ObterClientePorIdUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ClientesRepository.Returns(_clientesRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ObterClientePorIdUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new ObterClientePorIdEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _clientesRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterClientePorId()
        {
            // Arrange
            var entrada = new ObterClientePorIdEntrada(123456);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente>.Falha("ERRO_AO_OBTER_CLIENTE"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_CLIENTE");

            await _clientesRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoClienteNaoForEncontrado()
        {
            // Arrange
            var entrada = new ObterClientePorIdEntrada(123456);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente>.Falha("CLIENTE_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_NAO_ENCONTRADO");

            await _clientesRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveObterClienteComSucesso_QuandoIdForValido()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new ObterClientePorIdEntrada(cliente.Id.Valor);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente>.Sucesso(cliente));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(cliente.Id.Valor);
            resultado.Instancia.Nome.Should().Be(cliente.Nome.Valor);
            resultado.Instancia.Documento.Should().Be(cliente.Documento.Valor);
            resultado.Instancia.Email.Should().Be(cliente.Email.Valor);
            resultado.Instancia.Ativo.Should().Be(cliente.Ativo);
            resultado.Instancia.DataCriacaoUtc.Should().Be(cliente.DataCriacaoUtc);
            resultado.Instancia.DataAtualizacaoUtc.Should().Be(cliente.DataAtualizacaoUtc);

            resultado.Instancia.Endereco.Should().NotBeNull();
            resultado.Instancia.Endereco.Rua.Should().Be(cliente.Endereco.Rua);
            resultado.Instancia.Endereco.Numero.Should().Be(cliente.Endereco.Numero);
            resultado.Instancia.Endereco.Bairro.Should().Be(cliente.Endereco.Bairro);
            resultado.Instancia.Endereco.Cidade.Should().Be(cliente.Endereco.Cidade);
            resultado.Instancia.Endereco.Estado.Should().Be(cliente.Endereco.Estado);
            resultado.Instancia.Endereco.Cep.Should().Be(cliente.Endereco.Cep);
            resultado.Instancia.Endereco.Complemento.Should().Be(cliente.Endereco.Complemento);
            resultado.Instancia.Endereco.Pais.Should().Be(cliente.Endereco.Pais);

            await _clientesRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == cliente.Id.Valor),
                    Arg.Any<CancellationToken>());
        }
    }
}
