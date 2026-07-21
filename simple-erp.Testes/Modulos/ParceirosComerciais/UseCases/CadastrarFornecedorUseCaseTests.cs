using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class CadastrarFornecedorUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly CadastrarFornecedorUseCase _useCase;

        public CadastrarFornecedorUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();
            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);
            _useCase = new CadastrarFornecedorUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new CadastrarFornecedorEntrada(
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

            await _fornecedoresRepository
                .DidNotReceive()
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoVerificarExistenciaPorDocumento()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_CONSULTAR_FORNECEDOR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_CONSULTAR_FORNECEDOR");

            await _fornecedoresRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoJaExistirFornecedorComMesmoDocumento()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_JA_CADASTRADO");

            await _fornecedoresRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _fornecedoresRepository
                .AdicionarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");

            await _fornecedoresRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Fornecedor>(f =>
                        f.Nome.Valor == entrada.Nome &&
                        f.Documento.Valor == entrada.Documento &&
                        f.Email.Valor == entrada.Email &&
                        f.Ativo &&
                        f.Endereco.Rua == entrada.Rua &&
                        f.Endereco.Numero == entrada.Numero &&
                        f.Endereco.Complemento == entrada.Complemento &&
                        f.Endereco.Bairro == entrada.Bairro &&
                        f.Endereco.Cidade == entrada.Cidade &&
                        f.Endereco.Estado == entrada.Estado &&
                        f.Endereco.Cep == entrada.Cep &&
                        f.Endereco.Pais == entrada.Pais),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCadastrarFornecedorComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorDocumentoAsync(Arg.Any<Documento>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _fornecedoresRepository
                .AdicionarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().NotBeNull();

            resultado.Instancia.Id.Should().BeGreaterThan(0);
            resultado.Instancia.Nome.Should().Be(entrada.Nome);
            resultado.Instancia.Email.Should().Be(entrada.Email);
            resultado.Instancia.Ativo.Should().BeTrue();

            await _fornecedoresRepository
                .Received(1)
                .ExistePorDocumentoAsync(
                    Arg.Is<Documento>(d => d.Valor == entrada.Documento),
                    Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Fornecedor>(f =>
                        f.Nome.Valor == entrada.Nome &&
                        f.Documento.Valor == entrada.Documento &&
                        f.Email.Valor == entrada.Email &&
                        f.Ativo &&
                        f.Endereco.Rua == entrada.Rua &&
                        f.Endereco.Numero == entrada.Numero &&
                        f.Endereco.Complemento == entrada.Complemento &&
                        f.Endereco.Bairro == entrada.Bairro &&
                        f.Endereco.Cidade == entrada.Cidade &&
                        f.Endereco.Estado == entrada.Estado &&
                        f.Endereco.Cep == entrada.Cep &&
                        f.Endereco.Pais == entrada.Pais),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        private static CadastrarFornecedorEntrada CriarEntradaValida()
        {
            var endereco = EnderecoBuilder.Novo().Criar();

            return new CadastrarFornecedorEntrada(
                Documento: "11222333000181",
                Nome: "Fornecedor Teste",
                Email: "fornecedor@teste.com",
                Rua: endereco.Rua,
                Numero: endereco.Numero,
                Complemento: endereco.Complemento,
                Bairro: endereco.Bairro,
                Cidade: endereco.Cidade,
                Estado: endereco.Estado,
                Cep: endereco.Cep,
                Pais: endereco.Pais);
        }
    }
}
