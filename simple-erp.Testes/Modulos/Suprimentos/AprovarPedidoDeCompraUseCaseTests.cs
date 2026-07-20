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
    public sealed class AprovarPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly AprovarPedidoDeCompraUseCase _useCase;

        public AprovarPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new AprovarPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeCompraEntrada(0));

            resultado.EhFalha.Should().BeTrue();

            await _pedidosRepository
                .DidNotReceive()
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoNaoForEncontrado()
        {
            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Falha("PEDIDO_DE_COMPRA_NAO_ENCONTRADO"));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeCompraEntrada(202604020003));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoNaoPossuirItens()
        {
            var pedido = PedidoDeCompraBuilder.Novo().SemItens().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_SEM_ITENS");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveAprovarComSucesso_QuandoPedidoEmEdicaoComItens()
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

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Aprovada");
            resultado.Instancia.ValorTotal.Should().Be(50.00m);

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeCompra>(p => p.EstaAprovado), Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
