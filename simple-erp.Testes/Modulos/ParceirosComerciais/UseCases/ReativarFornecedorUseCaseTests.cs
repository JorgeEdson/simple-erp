using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class ReativarFornecedorUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly ReativarFornecedorUseCase _useCase;

        public ReativarFornecedorUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ReativarFornecedorUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new ReativarFornecedorEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _fornecedoresRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterFornecedorPorId()
        {
            // Arrange
            var entrada = new ReativarFornecedorEntrada(123456);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Falha("ERRO_AO_OBTER_FORNECEDOR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_FORNECEDOR");

            await _fornecedoresRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoFornecedorNaoForEncontrado()
        {
            // Arrange
            var entrada = new ReativarFornecedorEntrada(123456);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Falha("FORNECEDOR_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_NAO_ENCONTRADO");

            await _fornecedoresRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoAtualizarFalhar()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .Inativo()
                .Criar();

            var entrada = new ReativarFornecedorEntrada(fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_ATUALIZAR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_ATUALIZAR");

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .Inativo()
                .Criar();

            var entrada = new ReativarFornecedorEntrada(fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>())
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
                .AtualizarAsync(
                    Arg.Is<Fornecedor>(f =>
                        f.Id.Valor == fornecedor.Id.Valor &&
                        f.Ativo == true),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveReativarFornecedorComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .Inativo()
                .Criar();

            var entrada = new ReativarFornecedorEntrada(fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(fornecedor.Id.Valor);
            resultado.Instancia.Ativo.Should().BeTrue();

            await _fornecedoresRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == fornecedor.Id.Valor),
                    Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Fornecedor>(f =>
                        f.Id.Valor == fornecedor.Id.Valor &&
                        f.Ativo == true),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
