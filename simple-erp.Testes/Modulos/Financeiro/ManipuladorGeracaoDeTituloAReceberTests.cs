using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Handlers;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.Vendas.Eventos;
using System;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Financeiro
{
    public sealed class ManipuladorGeracaoDeTituloAReceberTests
    {
        private const long IdPedido = 202604020500;
        private const long IdCliente = 202604020001;

        private readonly IEmitirTituloAReceberUseCase _emitir =
            Substitute.For<IEmitirTituloAReceberUseCase>();
        private readonly ILogService _logService = Substitute.For<ILogService>();
        private readonly GeracaoDeTituloAReceberHandler _handler;

        public ManipuladorGeracaoDeTituloAReceberTests()
        {
            _handler = new GeracaoDeTituloAReceberHandler(_emitir, _logService);
        }

        private static PedidoDeVendaAprovado Evento(decimal valorTotal = 150.00m) =>
            new(
                Id.TentarCriar(IdPedido).Instancia,
                Id.TentarCriar(IdCliente).Instancia,
                valorTotal: valorTotal,
                itens: new List<ItemVendaAprovado>
                {
                    new(202604020010, 3m)
                });

        [Fact]
        public async Task ManipularAsync_DeveEmitirTituloAReceber_ComClienteEValorDoPedido()
        {
            _emitir
                .ExecutarAsync(Arg.Any<EmitirTituloAReceberEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<EmitirTituloAReceberSaida>.Sucesso(
                    new EmitirTituloAReceberSaida(1, "AReceber", IdCliente, 150.00m, 150.00m, "EmAberto", DateTime.UtcNow.AddDays(30))));

            var resultado = await _handler.ManipularAsync(Evento(150.00m));

            resultado.EhSucesso.Should().BeTrue();

            await _emitir
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<EmitirTituloAReceberEntrada>(e =>
                        e.IdCliente == IdCliente &&
                        e.Valor == 150.00m &&
                        e.IdPedidoDeVenda == IdPedido),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_DeveRetornarFalha_QuandoEmissaoFalhar()
        {
            _emitir
                .ExecutarAsync(Arg.Any<EmitirTituloAReceberEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<EmitirTituloAReceberSaida>.Falha("CLIENTE_NAO_ENCONTRADO"));

            var resultado = await _handler.ManipularAsync(Evento());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_NAO_ENCONTRADO");
        }
    }
}
