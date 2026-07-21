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

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos.UseCases
{
    public sealed class EditarProdutoUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly EditarProdutoUseCase _useCase;

        public EditarProdutoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);
            _useCase = new EditarProdutoUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoDadosDeEntradaForemInvalidos()
        {
            // Arrange
            var entrada = new EditarProdutoEntrada(
                Id: 0,
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
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _produtosRepository
                .DidNotReceive()
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>());

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
            var entrada = CriarEntradaValida();

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
            var entrada = CriarEntradaValida();

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Falha("PRODUTO_NAO_ENCONTRADO"));

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
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoCodigoForAlteradoEOcorrerErroAoVerificarDuplicidade()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .ComCodigo("PROD-001")
                .Criar();

            var entrada = CriarEntradaValida(
                id: produto.Id.Valor,
                codigo: "PROD-002");

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("ERRO_AO_VERIFICAR_CODIGO"));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_VERIFICAR_CODIGO");

            await _produtosRepository
                .Received(1)
                .ExisteOutroPorCodigoAsync(
                    Arg.Is<Id>(id => id.Valor == produto.Id.Valor),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoCodigoForAlteradoEJaExistirOutroProduto()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .ComCodigo("PROD-001")
                .Criar();

            var entrada = CriarEntradaValida(
                id: produto.Id.Valor,
                codigo: "PROD-002");

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            // Act
            var resultado = await _useCase.ExecutarAsync(entrada);

            // Assert
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_JA_CADASTRADO");

            await _produtosRepository
                .Received(1)
                .ExisteOutroPorCodigoAsync(
                    Arg.Is<Id>(id => id.Valor == produto.Id.Valor),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEditarComSucesso_QuandoCodigoNaoForAlterado()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .ComCodigo("PROD-001")
                .Criar();

            var entrada = CriarEntradaValida(
                id: produto.Id.Valor,
                codigo: produto.Codigo.Valor,
                descricao: "Produto Editado",
                unidadeDeMedida: "KG");

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
            resultado.Instancia.Descricao.Should().Be("Produto Editado");
            resultado.Instancia.UnidadeDeMedida.Should().Be("KG");

            await _produtosRepository
                .DidNotReceive()
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Produto>(p =>
                        p.Id.Valor == produto.Id.Valor &&
                        p.Codigo.Valor == entrada.Codigo &&
                        p.Descricao.Valor == entrada.Descricao &&
                        p.UnidadeDeMedida.Valor == entrada.UnidadeDeMedida),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoAtualizarFalhar()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .Criar();

            var entrada = CriarEntradaValida(id: produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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

            var entrada = CriarEntradaValida(id: produto.Id.Valor);

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
                .AtualizarAsync(Arg.Any<Produto>(), Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEditarProdutoComSucesso_QuandoDadosForemValidos()
        {
            // Arrange
            var produto = ProdutoBuilder.Novo()
                .ComId(123456)
                .ComCodigo("PROD-001")
                .Criar();

            var entrada = CriarEntradaValida(
                id: produto.Id.Valor,
                codigo: "PROD-NEW",
                descricao: "Produto Atualizado",
                unidadeDeMedida: "L");

            _produtosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto?>.Sucesso(produto));

            _produtosRepository
                .ExisteOutroPorCodigoAsync(
                    Arg.Any<Id>(),
                    Arg.Any<CodigoProduto>(),
                    Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

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
            resultado.Instancia.Codigo.Should().Be("PROD-NEW");
            resultado.Instancia.Descricao.Should().Be("Produto Atualizado");
            resultado.Instancia.UnidadeDeMedida.Should().Be("L");
            resultado.Instancia.Ativo.Should().BeTrue();

            await _produtosRepository
                .Received(1)
                .ExisteOutroPorCodigoAsync(
                    Arg.Is<Id>(id => id.Valor == produto.Id.Valor),
                    Arg.Is<CodigoProduto>(c => c.Valor == entrada.Codigo),
                    Arg.Any<CancellationToken>());

            await _produtosRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<Produto>(p =>
                        p.Id.Valor == produto.Id.Valor &&
                        p.Codigo.Valor == entrada.Codigo &&
                        p.Descricao.Valor == entrada.Descricao &&
                        p.UnidadeDeMedida.Valor == entrada.UnidadeDeMedida),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        private static EditarProdutoEntrada CriarEntradaValida(
            long id = 123456,
            string codigo = "PROD-EDIT",
            string descricao = "Produto Editado",
            string unidadeDeMedida = "UN")
        {
            return new EditarProdutoEntrada(
                Id: id,
                Codigo: codigo,
                Descricao: descricao,
                UnidadeDeMedida: unidadeDeMedida);
        }
    }
}
