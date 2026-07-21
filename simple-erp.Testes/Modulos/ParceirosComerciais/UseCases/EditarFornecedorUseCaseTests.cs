using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.UseCases
{
    public sealed class EditarFornecedorUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly EditarFornecedorUseCase _useCase;

        public EditarFornecedorUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();
            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);
            _useCase = new EditarFornecedorUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new EditarFornecedorEntrada(
                Id: 0,
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
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .DidNotReceive()
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>());

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
            var entrada = CriarEntradaValida();

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
            var entrada = CriarEntradaValida();

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
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoVerificarDuplicidadeDeDocumento()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .ComDocumento("11222333000181")
                .Criar();

            var entrada = CriarEntradaValida(
                id: fornecedor.Id.Valor,
                documento: "99888777000166");

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_VERIFICAR_DOCUMENTO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_VERIFICAR_DOCUMENTO");

            await _fornecedoresRepository
                .Received(1)
                .ExisteOutroPorDocumentoAsync(
                    Arg.Is<Id>(id => id.Valor == fornecedor.Id.Valor),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoJaExistirOutroFornecedorComMesmoDocumento()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .ComDocumento("11222333000181")
                .Criar();

            var entrada = CriarEntradaValida(
                id: fornecedor.Id.Valor,
                documento: "99888777000166");

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("DOCUMENTO_JA_CADASTRADO");

            await _fornecedoresRepository
                .Received(1)
                .ExisteOutroPorDocumentoAsync(
                    Arg.Is<Id>(id => id.Valor == fornecedor.Id.Valor),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>());

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
                .Criar();

            var entrada = CriarEntradaValida(id: fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
                .Criar();

            var entrada = CriarEntradaValida(id: fornecedor.Id.Valor);

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
                .AtualizarAsync(Arg.Any<Fornecedor>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEditarFornecedorComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(123456)
                .ComDocumento("11222333000181")
                .Criar();

            var entrada = CriarEntradaValida(
                id: fornecedor.Id.Valor,
                documento: "99888777000166",
                nome: "Fornecedor Atualizado",
                email: "fornecedor.atualizado@teste.com");

            _fornecedoresRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Fornecedor>.Sucesso(fornecedor));

            _fornecedoresRepository
                .ExisteOutroPorDocumentoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<Documento>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
            resultado.Instancia.Nome.Should().Be("Fornecedor Atualizado");
            resultado.Instancia.Email.Should().Be("fornecedor.atualizado@teste.com");
            resultado.Instancia.Ativo.Should().BeTrue();

            await _fornecedoresRepository
                .Received(1)
                .ExisteOutroPorDocumentoAsync(
                    Arg.Is<Id>(id => id.Valor == fornecedor.Id.Valor),
                    Arg.Is<Documento>(d => d.Valor == entrada.Documento),
                    Arg.Any<CancellationToken>());

            await _fornecedoresRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Fornecedor>(f =>
                        f.Id.Valor == fornecedor.Id.Valor &&
                        f.Documento.Valor == entrada.Documento &&
                        f.Nome.Valor == entrada.Nome &&
                        f.Email.Valor == entrada.Email &&
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

        private static EditarFornecedorEntrada CriarEntradaValida(
            long id = 123456,
            string documento = "99888777000166",
            string nome = "Fornecedor Editado",
            string email = "fornecedor.editado@teste.com")
        {
            var endereco = EnderecoBuilder.Novo().Criar();

            return new EditarFornecedorEntrada(
                Id: id,
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
