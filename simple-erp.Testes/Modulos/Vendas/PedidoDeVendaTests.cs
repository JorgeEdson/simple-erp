using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.Eventos;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System.Linq;

namespace simple_erp.Testes.Modulos.Vendas
{
    public sealed class PedidoDeVendaTests
    {
        private static MotivoCancelamento Motivo(string texto = "Cliente desistiu") =>
            MotivoCancelamento.TentarCriar(texto).Instancia;

        [Fact]
        public void Criar_DeveIniciarEmEdicaoEEmitirEvento()
        {
            var idCliente = Id.TentarCriar(202604020002).Instancia;

            var resultado = PedidoDeVenda.Criar(1, idCliente);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.EstaEmEdicao.Should().BeTrue();
            resultado.Instancia.Numero.Should().Be(1);
            resultado.Instancia.EventosDeDominio.OfType<PedidoDeVendaCriado>().Should().ContainSingle();
        }

        [Fact]
        public void ValorTotal_DeveConsiderarDescontosDeItemEDePedido()
        {
            var pedido = PedidoDeVendaBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 2m, 10.00m, desconto: 2.00m) // bruto 20, subtotal 18
                .ComItem(202604020002, 1m, 30.00m)                   // bruto 30, subtotal 30
                .ComDescontoDoPedido(8.00m)                          // total 48 - 8 = 40
                .Criar();

            pedido.ValorBrutoItens.Should().Be(50.00m);
            pedido.ValorSubtotalItens.Should().Be(48.00m);
            pedido.ValorTotal.Valor.Should().Be(40.00m);
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoDescontoDoPedidoExcederSubtotal()
        {
            var idCliente = Id.TentarCriar(202604020002).Instancia;
            var item = ItemDePedidoDeVenda.TentarCriar(
                Id.TentarCriar(202604020001).Instancia,
                Quantidade.TentarCriar(1m).Instancia,
                Dinheiro.TentarCriar(10.00m).Instancia,
                Dinheiro.TentarCriar(0m).Instancia).Instancia;

            var descontoExcessivo = Dinheiro.TentarCriar(20.00m).Instancia;

            var resultado = PedidoDeVenda.Criar(1, idCliente, new[] { item }, descontoExcessivo);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("DESCONTO_PEDIDO_INVALIDO");
        }

        [Fact]
        public void AdicionarItem_DeveFalhar_QuandoPedidoNaoEstiverEmEdicao()
        {
            var pedido = PedidoDeVendaBuilder.Novo().Aprovado().Criar();

            var item = ItemDePedidoDeVenda.TentarCriar(
                Id.TentarCriar(202604020099).Instancia,
                Quantidade.TentarCriar(1m).Instancia,
                Dinheiro.TentarCriar(5.00m).Instancia,
                Dinheiro.TentarCriar(0m).Instancia).Instancia;

            var resultado = pedido.AdicionarItem(item);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_VENDA_NAO_EDITAVEL");
        }

        [Fact]
        public void Aprovar_DeveCongelarValoresEEmitirEventoComItens()
        {
            var pedido = PedidoDeVendaBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 2m, 10.00m)
                .Criar();

            var resultado = pedido.Aprovar();

            resultado.EhSucesso.Should().BeTrue();
            pedido.EstaAprovado.Should().BeTrue();

            var evento = pedido.EventosDeDominio.OfType<PedidoDeVendaAprovado>().Should().ContainSingle().Subject;
            evento.Itens.Should().ContainSingle(i => i.IdProduto == 202604020001 && i.Quantidade == 2m);
            evento.ValorTotal.Should().Be(20.00m);
        }

        [Fact]
        public void Cancelar_DeveFalhar_QuandoPedidoConcluido()
        {
            var pedido = PedidoDeVendaBuilder.Novo().Concluido().Criar();

            var resultado = pedido.Cancelar(Motivo());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_VENDA_CONCLUIDO_NAO_PODE_SER_CANCELADO");
        }

        [Fact]
        public void Cancelar_DeveRegistrarMotivoEEmitirEvento()
        {
            var pedido = PedidoDeVendaBuilder.Novo().Criar();

            var resultado = pedido.Cancelar(Motivo("Fora de área de entrega"));

            resultado.EhSucesso.Should().BeTrue();
            pedido.EstaCancelado.Should().BeTrue();
            pedido.MotivoCancelamento.Should().Be("Fora de área de entrega");
            pedido.EventosDeDominio.OfType<PedidoDeVendaCancelado>().Should().ContainSingle();
        }
    }
}
