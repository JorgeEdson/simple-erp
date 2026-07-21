using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class ObterFornecedorPorIdUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly ObterFornecedorPorIdUseCase _useCase;

        public ObterFornecedorPorIdUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ObterFornecedorPorIdUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new ObterFornecedorPorIdEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _fornecedoresRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterFornecedorPorId()
        {
            // Arrange
            var entrada = new ObterFornecedorPorIdEntrada(123456);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Falha("ERRO_AO_OBTER_FORNECEDOR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_FORNECEDOR");

            await _fornecedoresRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoFornecedorNaoForEncontrado()
        {
            // Arrange
            var entrada = new ObterFornecedorPorIdEntrada(123456);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Falha("FORNECEDOR_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_NAO_ENCONTRADO");

            await _fornecedoresRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveObterFornecedorComSucesso_QuandoIdForValido()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new ObterFornecedorPorIdEntrada(fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(fornecedor.Id.Valor);
            resultado.Instancia.Nome.Should().Be(fornecedor.Nome.Valor);
            resultado.Instancia.Documento.Should().Be(fornecedor.Documento.Valor);
            resultado.Instancia.Email.Should().Be(fornecedor.Email.Valor);
            resultado.Instancia.Ativo.Should().Be(fornecedor.Ativo);
            resultado.Instancia.DataCriacaoUtc.Should().Be(fornecedor.DataCriacaoUtc);
            resultado.Instancia.DataAtualizacaoUtc.Should().Be(fornecedor.DataAtualizacaoUtc);

            resultado.Instancia.Endereco.Should().NotBeNull();
            resultado.Instancia.Endereco.Rua.Should().Be(fornecedor.Endereco.Rua);
            resultado.Instancia.Endereco.Numero.Should().Be(fornecedor.Endereco.Numero);
            resultado.Instancia.Endereco.Complemento.Should().Be(fornecedor.Endereco.Complemento);
            resultado.Instancia.Endereco.Bairro.Should().Be(fornecedor.Endereco.Bairro);
            resultado.Instancia.Endereco.Cidade.Should().Be(fornecedor.Endereco.Cidade);
            resultado.Instancia.Endereco.Estado.Should().Be(fornecedor.Endereco.Estado);
            resultado.Instancia.Endereco.Cep.Should().Be(fornecedor.Endereco.Cep);
            resultado.Instancia.Endereco.Pais.Should().Be(fornecedor.Endereco.Pais);

            await _fornecedoresRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == fornecedor.Id.Valor),
                    Arg.Any<CancellationToken>());
        }
    }
}
