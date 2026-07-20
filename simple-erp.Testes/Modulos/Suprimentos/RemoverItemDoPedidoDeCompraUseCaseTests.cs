using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class RemoverItemDoPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly RemoverItemDoPedidoDeCompraUseCase _useCase;

        public RemoverItemDoPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new RemoverItemDoPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoItemNaoExistir()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(
                new RemoverItemDoPedidoDeCompraEntrada(pedido.Id.Valor, IdProduto: 999888777));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ITEM_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRemoverItemComSucesso_QuandoItemExistir()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .ComItem(202604020010, 4m, 2.00m)
                .Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(
                new RemoverItemDoPedidoDeCompraEntrada(pedido.Id.Valor, IdProduto: 202604020010));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.QuantidadeItens.Should().Be(1);
            resultado.Instancia.ValorTotal.Should().Be(50.00m);

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeCompra>(p => p.Itens.Count == 1), Arg.Any<CancellationToken>());
        }
    }
}
