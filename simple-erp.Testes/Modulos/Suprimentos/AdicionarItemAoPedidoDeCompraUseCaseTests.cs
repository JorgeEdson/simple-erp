using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class AdicionarItemAoPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly AdicionarItemAoPedidoDeCompraUseCase _useCase;

        public AdicionarItemAoPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new AdicionarItemAoPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        private AdicionarItemAoPedidoDeCompraEntrada EntradaValida(long idPedido) =>
            new(idPedido, IdProduto: 202604020055, Quantidade: 2m, CustoUnitario: 3.00m);

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoExistir()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _produtosRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(EntradaValida(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoNaoEstiverEmEdicao()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _produtosRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            var resultado = await _useCase.ExecutarAsync(EntradaValida(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_EDITAVEL");
        }

        [Fact]
        public async Task ExecutarAsync_DeveAdicionarItemComSucesso_QuandoDadosForemValidos()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _produtosRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(EntradaValida(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.QuantidadeItens.Should().Be(2);
            resultado.Instancia.ValorTotal.Should().Be(56.00m); // 50,00 + (2 x 3,00)

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeCompra>(p => p.Itens.Count == 2), Arg.Any<CancellationToken>());
        }
    }
}
