using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos
{
    public sealed class ObterProdutoPorIdUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly ObterProdutoPorIdUseCase _useCase;

        public ObterProdutoPorIdUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _logService
                .IniciarEscopo(Arg.Any<Dictionary<string, object?>>())
                .Returns(Substitute.For<IDisposable>());

            _useCase = new ObterProdutoPorIdUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            // Arrange
            var entrada = new ObterProdutoPorIdEntrada(0);

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().NotBeNullOrEmpty();

            await _produtosRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOcorrerErroAoObterProdutoPorId()
        {
            // Arrange
            var entrada = new ObterProdutoPorIdEntrada(123456);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Falha("ERRO_AO_OBTER_PRODUTO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_OBTER_PRODUTO");

            await _produtosRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoForEncontrado()
        {
            // Arrange
            var entrada = new ObterProdutoPorIdEntrada(123456);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Falha("PRODUTO_NAO_ENCONTRADO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");

            await _produtosRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == entrada.Id),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveObterProdutoComSucesso_QuandoIdForValido()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .ComoFabricado()
                .Criar();

            var entrada = new ObterProdutoPorIdEntrada(produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Sucesso(produto));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(produto.Id.Valor);
            resultado.Instancia.Codigo.Should().Be(produto.Codigo.Valor);
            resultado.Instancia.Descricao.Should().Be(produto.Descricao.Valor);
            resultado.Instancia.UnidadeDeMedida.Should().Be(produto.UnidadeDeMedida.Valor);
            resultado.Instancia.Classificacao.Should().Be(produto.Classificacao.ToString());
            resultado.Instancia.Ativo.Should().Be(produto.Ativo);
            resultado.Instancia.DataCriacaoUtc.Should().Be(produto.DataCriacaoUtc);
            resultado.Instancia.DataAtualizacaoUtc.Should().Be(produto.DataAtualizacaoUtc);

            await _produtosRepository
                .Received(1)
                .ObterPorIdAsync(
                    Arg.Is<Id>(id => id.Valor == produto.Id.Valor),
                    Arg.Any<CancellationToken>());
        }
    }
}
