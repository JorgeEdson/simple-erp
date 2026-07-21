using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Handlers;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using System;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Financeiro
{
    public sealed class ManipuladorGeracaoDeTituloAPagarTests
    {
        private const long IdPedido = 202604020003;
        private const long IdFornecedor = 202604020002;

        private readonly IEmitirTituloAPagarUseCase _emitir =
            Substitute.For<IEmitirTituloAPagarUseCase>();
        private readonly ILogService _logService = Substitute.For<ILogService>();
        private readonly GeracaoDeTituloAPagarHandler _handler;

        public ManipuladorGeracaoDeTituloAPagarTests()
        {
            _handler = new GeracaoDeTituloAPagarHandler(_emitir, _logService);
        }

        private static PedidoDeCompraEfetivado Evento(decimal valorTotal = 44.00m) =>
            new(
                Id.TentarCriar(IdPedido).Instancia,
                Id.TentarCriar(IdFornecedor).Instancia,
                valorTotal: valorTotal,
                itens: new List<ItemPedidoDeCompraEfetivado>
                {
                    new(202604020010, 2m, 22.00m)
                });

        [Fact]
        public async Task ManipularAsync_DeveEmitirTituloAPagar_ComFornecedorEValorDoPedido()
        {
            _emitir
                .ExecutarAsync(Arg.Any<EmitirTituloAPagarEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<EmitirTituloAPagarSaida>.Sucesso(
                    new EmitirTituloAPagarSaida(1, "APagar", IdFornecedor, 44.00m, 44.00m, "EmAberto", DateTime.UtcNow.AddDays(30))));

            var resultado = await _handler.ManipularAsync(Evento(44.00m));

            resultado.EhSucesso.Should().BeTrue();

            await _emitir
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<EmitirTituloAPagarEntrada>(e =>
                        e.IdFornecedor == IdFornecedor &&
                        e.Valor == 44.00m &&
                        e.IdPedidoDeCompra == IdPedido),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_DeveRetornarFalha_QuandoEmissaoFalhar()
        {
            _emitir
                .ExecutarAsync(Arg.Any<EmitirTituloAPagarEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<EmitirTituloAPagarSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO"));

            var resultado = await _handler.ManipularAsync(Evento());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_NAO_ENCONTRADO");
        }
    }
}
