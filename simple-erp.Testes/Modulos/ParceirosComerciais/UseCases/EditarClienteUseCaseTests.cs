using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class EditarClienteUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClienteRepository _clientesRepository;
        private readonly ILogService _logService;
        private readonly EditarClienteUseCase _useCase;

        public EditarClienteUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _logService = Substitute.For<ILogService>();
            _unitOfWork.ClientesRepository.Returns(_clientesRepository);
            _useCase = new EditarClienteUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new EditarClienteEntrada(
                Id: Guid.Empty,
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
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

            await _clientesRepository
                .DidNotReceive()
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>());

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
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
            var entrada = CriarEntradaValida();

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente>.Falha("CLIENTE_NAO_ENCONTRADO"));

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
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDocumentoForAlteradoEOcorrerErroAoVerificarDuplicidade()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .ComDocumento("12345678909")
                .Criar();

            var entrada = CriarEntradaValida(
                id: cliente.Id,
                documento: "98765432100");

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_VERIFICAR_DOCUMENTO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_VERIFICAR_DOCUMENTO");

            await _clientesRepository
                .Received(1)
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDocumentoForAlteradoEJaExistirOutroCliente()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .ComDocumento("12345678909")
                .Criar();

            var entrada = CriarEntradaValida(
                id: cliente.Id,
                documento: "98765432100");

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_JA_CADASTRADO");

            await _clientesRepository
                .Received(1)
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEditarComSucesso_QuandoDocumentoNaoForAlterado()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .ComDocumento("12345678909")
                .Criar();

            var entrada = CriarEntradaValida(
                id: cliente.Id,
                documento: cliente.Documento.Valor,
                nome: "Cliente Editado",
                email: "editado@teste.com");

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
            resultado.Instancia.Nome.Should().Be("Cliente Editado");
            resultado.Instancia.Email.Should().Be("editado@teste.com");

            await _clientesRepository
                .DidNotReceive()
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Id == cliente.Id &&
                        c.Nome.Valor == entrada.Nome &&
                        c.Email.Valor == entrada.Email &&
                        c.Documento.Valor == entrada.Documento),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoAtualizarFalhar()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .Criar();

            var entrada = CriarEntradaValida(id: cliente.Id);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .Criar();

            var entrada = CriarEntradaValida(id: cliente.Id);

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
                .AtualizarAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEditarClienteComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var cliente = ClienteBuilder.Novo()
                .ComId(new Guid("00000000-0000-0000-0000-000000123456"))
                .ComDocumento("12345678909")
                .Criar();

            var entrada = CriarEntradaValida(
                id: cliente.Id,
                documento: "98765432100",
                nome: "Cliente Atualizado",
                email: "cliente.atualizado@teste.com");

            _clientesRepository
                .ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Cliente?>.Sucesso(cliente));

            _clientesRepository
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
            resultado.Instancia.Id.Should().Be(cliente.Id);
            resultado.Instancia.Nome.Should().Be("Cliente Atualizado");
            resultado.Instancia.Email.Should().Be("cliente.atualizado@teste.com");
            resultado.Instancia.Ativo.Should().BeTrue();

            await _clientesRepository
                .Received(1)
                .ExisteAsync(
                    Arg.Any<ISpecification<Cliente>>(),
                    Arg.Any<CancellationToken>());

            await _clientesRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Cliente>(c =>
                        c.Id == cliente.Id &&
                        c.Documento.Valor == entrada.Documento &&
                        c.Nome.Valor == entrada.Nome &&
                        c.Email.Valor == entrada.Email &&
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

        private static EditarClienteEntrada CriarEntradaValida(
            Guid? id = null,
            string documento = "98765432100",
            string nome = "Cliente Editado",
            string email = "cliente.editado@teste.com")
        {
            var endereco = EnderecoBuilder.Novo().Criar();

            return new EditarClienteEntrada(
                Id: id ?? new Guid("00000000-0000-0000-0000-000000123456"),
                Documento: documento,
                Nome: nome,
                Email: email,
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
