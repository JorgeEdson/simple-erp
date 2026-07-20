using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Eventos;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using System.Collections.Generic;

namespace simple_erp.Testes.Compartilhado
{
    public sealed class DispatcherDeEventosTests
    {
        private readonly ILogService _logService = Substitute.For<ILogService>();

        private static PedidoDeCompraCancelado Evento(long id = 202604020003) =>
            new(Id.TentarCriar(id).Instancia);

        /// <summary>Handler concreto (spy) para exercitar o double-dispatch real do dispatcher.</summary>
        private sealed class HandlerEspiao : IManipuladorDeEventoDeDominio<PedidoDeCompraCancelado>
        {
            private readonly Resultado<bool> _retorno;

            public HandlerEspiao(Resultado<bool>? retorno = null)
            {
                _retorno = retorno ?? Resultado<bool>.Sucesso(true);
            }

            public int Invocacoes { get; private set; }

            public Task<Resultado<bool>> ManipularAsync(
                PedidoDeCompraCancelado evento,
                CancellationToken cancellationToken = default)
            {
                Invocacoes++;
                return Task.FromResult(_retorno);
            }
        }

        [Fact]
        public async Task DespacharAsync_DeveInvocarTodosOsHandlersDoEvento_FanOut()
        {
            var handlerA = new HandlerEspiao();
            var handlerB = new HandlerEspiao();

            var resolvedor = new ResolvedorDeManipuladoresEmMemoria()
                .Registrar<PedidoDeCompraCancelado>(handlerA)
                .Registrar<PedidoDeCompraCancelado>(handlerB);

            var dispatcher = new DispatcherDeEventos(resolvedor, _logService);

            var resultado = await dispatcher.DespacharAsync(new EventoDeDominio[] { Evento() });

            resultado.EhSucesso.Should().BeTrue();
            handlerA.Invocacoes.Should().Be(1);
            handlerB.Invocacoes.Should().Be(1);
        }

        [Fact]
        public async Task DespacharAsync_DeveAgregarFalha_QuandoUmHandlerFalhar()
        {
            var handlerOk = new HandlerEspiao(Resultado<bool>.Sucesso(true));
            var handlerFalho = new HandlerEspiao(Resultado<bool>.Falha("FALHA_NO_HANDLER"));

            var resolvedor = new ResolvedorDeManipuladoresEmMemoria()
                .Registrar<PedidoDeCompraCancelado>(handlerOk)
                .Registrar<PedidoDeCompraCancelado>(handlerFalho);

            var dispatcher = new DispatcherDeEventos(resolvedor, _logService);

            var resultado = await dispatcher.DespacharAsync(new EventoDeDominio[] { Evento() });

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FALHA_NO_HANDLER");
            handlerOk.Invocacoes.Should().Be(1); // os demais handlers continuam sendo executados
        }

        [Fact]
        public async Task DespacharAsync_DeveSerNoOp_QuandoNaoHouverHandlerRegistrado()
        {
            var dispatcher = new DispatcherDeEventos(new ResolvedorDeManipuladoresEmMemoria(), _logService);

            var resultado = await dispatcher.DespacharAsync(new EventoDeDominio[] { Evento() });

            resultado.EhSucesso.Should().BeTrue();
        }
    }
}
