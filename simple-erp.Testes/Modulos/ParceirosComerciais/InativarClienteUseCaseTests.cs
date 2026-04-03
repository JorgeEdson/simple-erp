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

namespace simple_erp.Testes.Modulos.ParceirosComerciais
{
    public sealed class InativarClienteUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClienteRepository _clientesRepository;
        private readonly ILogService _logService;
        private readonly InativarClienteUseCase _useCase;

        public InativarClienteUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ClientesRepository.Returns(_clientesRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new InativarClienteUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new InativarClienteEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _clientesRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _clientesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterClientePorId()
        {
            // Arrange
            var entrada = new InativarClienteEntrada(123456);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Falha("ERRO_AO_OBTER_CLIENTE"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_CLIENTE");

            await _clientesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoClienteNaoForEncontrado()
        {
            // Arrange
            var entrada = new InativarClienteEntrada(123456);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Falha("CLIENTE_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_NAO_ENCONTRADO");

            await _clientesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }      

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoAtualizarFalhar()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new InativarClienteEntrada(cliente.Id.Valor);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>())
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
            var cliente = ClienteBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new InativarClienteEntrada(cliente.Id.Valor);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>())
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
                .AtualizarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Id.Valor == cliente.Id.Valor &&
                        c.Ativo == false),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveInativarClienteComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new InativarClienteEntrada(cliente.Id.Valor);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(cliente.Id.Valor);
            resultado.Instancia.Ativo.Should().BeFalse();

            await _clientesRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == cliente.Id.Valor),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Id.Valor == cliente.Id.Valor &&
                        c.Ativo == false),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
