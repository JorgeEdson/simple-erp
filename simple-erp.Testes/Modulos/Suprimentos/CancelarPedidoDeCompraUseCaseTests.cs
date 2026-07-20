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
    public sealed class CancelarPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly CancelarPedidoDeCompraUseCase _useCase;

        public CancelarPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new CancelarPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoJaEstiverConcluido()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Concluido().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(new CancelarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_CONCLUIDO_NAO_PODE_SER_CANCELADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCancelarComSucesso_QuandoPedidoEmEdicao()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new CancelarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Cancelada");

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeCompra>(p => p.EstaCancelado), Arg.Any<CancellationToken>());
        }
    }
}
