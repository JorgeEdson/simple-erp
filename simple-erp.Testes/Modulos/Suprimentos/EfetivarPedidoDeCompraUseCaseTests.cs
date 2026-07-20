using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class EfetivarPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly IDispatcherDeEventos _dispatcher;
        private readonly EfetivarPedidoDeCompraUseCase _useCase;

        public EfetivarPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();
            _dispatcher = Substitute.For<IDispatcherDeEventos>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);
            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _useCase = new EfetivarPedidoDeCompraUseCase(_unitOfWork, _logService, _dispatcher);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoNaoEstiverAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar(); // em edição

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_APROVADO_NAO_PODE_SER_EFETIVADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEfetivarEDespacharEvento_QuandoPedidoEstiverAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");

            // O evento PedidoDeCompraEfetivado é despachado após a persistência.
            await _dispatcher
                .Received(1)
                .DespacharAsync(
                    Arg.Is<IEnumerable<EventoDeDominio>>(eventos =>
                        eventos.OfType<PedidoDeCompraEfetivado>().Any()),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarSucesso_MesmoQuandoHandlerFalhar()
        {
            // Consistência eventual: falha de handler não desfaz a efetivação já persistida.
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));
            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));
            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("FALHA_EM_HANDLER"));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");

            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }
    }
}
