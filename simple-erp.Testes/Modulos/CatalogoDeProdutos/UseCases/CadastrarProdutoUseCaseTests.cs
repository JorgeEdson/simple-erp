using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos.UseCases
{
    public sealed class CadastrarProdutoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly CadastrarProdutoUseCase _useCase;

        public CadastrarProdutoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);
            _logService = Substitute.For<ILogService>();
            _useCase = new CadastrarProdutoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new CadastrarProdutoEntrada(
                Codigo: "",
                Descricao: "",
                UnidadeDeMedida: "INVALIDO");

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _produtosRepository
                .DidNotReceive()
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>());

            await _produtosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoClassificacaoInformadaForInvalida()
        {
            // Arrange
            var entrada = new CadastrarProdutoEntrada(
                Codigo: "PROD-001",
                Descricao: "Produto Teste",
                UnidadeDeMedida: "UN",
                Classificacao: "Imaginaria");

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLASSIFICACAO_PRODUTO_INVALIDA");

            await _produtosRepository
                .DidNotReceive()
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoVerificarExistenciaPorCodigo()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_CONSULTAR_PRODUTO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_CONSULTAR_PRODUTO");

            await _produtosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoJaExistirProdutoComMesmoCodigo()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_JA_CADASTRADO");

            await _produtosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _produtosRepository
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");

            await _produtosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Produto>(p =>
                        p.Codigo.Valor == entrada.Codigo &&
                        p.Descricao.Valor == entrada.Descricao &&
                        p.UnidadeDeMedida.Valor == entrada.UnidadeDeMedida &&
                        p.Ativo),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCadastrarProdutoComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _produtosRepository
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();

            resultado.Instancia.Should().NotBeNull();
            resultado.Instancia.Codigo.Should().Be(entrada.Codigo);
            resultado.Instancia.Descricao.Should().Be(entrada.Descricao);
            resultado.Instancia.UnidadeDeMedida.Should().Be(entrada.UnidadeDeMedida);
            resultado.Instancia.Classificacao.Should().Be(ClassificacaoProduto.SemClassificacao.ToString());
            resultado.Instancia.Ativo.Should().BeTrue();
            resultado.Instancia.Id.Should().BeGreaterThan(0);

            await _produtosRepository
                .Received(1)
                .ExistePorCodigoAsync(
                    Arg.Is<CodigoProduto>(c => c.Valor == entrada.Codigo),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Produto>(p =>
                        p.Codigo.Valor == entrada.Codigo &&
                        p.Descricao.Valor == entrada.Descricao &&
                        p.UnidadeDeMedida.Valor == entrada.UnidadeDeMedida &&
                        p.Ativo),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCadastrarProdutoComoFabricado_QuandoClassificacaoForFabricado()
        {
            // Arrange
            var entrada = CriarEntradaValida(classificacao: "Fabricado");

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _produtosRepository
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Classificacao.Should().Be(ClassificacaoProduto.Fabricado.ToString());

            await _produtosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Produto>(p => p.Classificacao == ClassificacaoProduto.Fabricado),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCadastrarProdutoComoMateriaPrima_QuandoClassificacaoForMateriaPrima()
        {
            // Arrange
            var entrada = CriarEntradaValida(classificacao: "MateriaPrima");

            _produtosRepository
                .ExistePorCodigoAsync(Arg.Any<CodigoProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            _produtosRepository
                .AdicionarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Classificacao.Should().Be(ClassificacaoProduto.MateriaPrima.ToString());

            await _produtosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Produto>(p => p.Classificacao == ClassificacaoProduto.MateriaPrima),
                    Arg.Any<CancellationToken>());
        }

        private static CadastrarProdutoEntrada CriarEntradaValida(
            string codigo = "PROD-001",
            string descricao = "Produto Teste",
            string unidadeDeMedida = "UN",
            string? classificacao = null)
        {
            return new CadastrarProdutoEntrada(
                Codigo: codigo,
                Descricao: descricao,
                UnidadeDeMedida: unidadeDeMedida,
                Classificacao: classificacao);
        }
    }
}
