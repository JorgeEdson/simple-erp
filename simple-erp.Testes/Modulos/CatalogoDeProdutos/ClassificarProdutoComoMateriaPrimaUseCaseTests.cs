using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos
{
    public sealed class ClassificarProdutoComoMateriaPrimaUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly ClassificarProdutoComoMateriaPrimaUseCase _useCase;

        public ClassificarProdutoComoMateriaPrimaUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ClassificarProdutoComoMateriaPrimaUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _produtosRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterProdutoPorId()
        {
            // Arrange
            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(123456);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Falha("ERRO_AO_OBTER_PRODUTO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_PRODUTO");

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoForEncontrado()
        {
            // Arrange
            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(123456);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Falha("PRODUTO_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoEstiverInativo()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .Inativo()
                .Criar();

            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_INATIVO_NAO_PODE_SER_CLASSIFICADO");

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoAtualizarFalhar()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
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
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
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
                .AtualizarAsync(
                    Arg.Is<Produto>(p =>
                        p.Id.Valor == produto.Id.Valor &&
                        p.Classificacao == ClassificacaoProduto.MateriaPrima),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveClassificarProdutoComoMateriaPrimaComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .SemClassificacao()
                .Criar();

            var entrada = new ClassificarProdutoComoMateriaPrimaEntrada(produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(produto.Id.Valor);
            resultado.Instancia.Classificacao.Should().Be(ClassificacaoProduto.MateriaPrima.ToString());
            resultado.Instancia.Ativo.Should().BeTrue();

            await _produtosRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == produto.Id.Valor),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Produto>(p =>
                        p.Id.Valor == produto.Id.Valor &&
                        p.Classificacao == ClassificacaoProduto.MateriaPrima),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
