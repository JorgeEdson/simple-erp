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
    public sealed class ObterPedidoDeCompraPorIdUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly ObterPedidoDeCompraPorIdUseCase _useCase;

        public ObterPedidoDeCompraPorIdUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new ObterPedidoDeCompraPorIdUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            var resultado = await _useCase.ExecutarAsync(new ObterPedidoDeCompraPorIdEntrada(0));

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

            var resultado = await _useCase.ExecutarAsync(new ObterPedidoDeCompraPorIdEntrada(202604020003));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_ENCONTRADO");
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarPedidoComItens_QuandoEncontrado()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(
                new ObterPedidoDeCompraPorIdEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Id.Should().Be(pedido.Id.Valor);
            resultado.Instancia.Status.Should().Be("EmEdicao");
            resultado.Instancia.ValorTotal.Should().Be(50.00m);
            resultado.Instancia.Itens.Should().ContainSingle(i => i.IdProduto == 202604020001 && i.Subtotal == 50.00m);
        }
    }
}
