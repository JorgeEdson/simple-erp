using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    /// <summary>
    /// Fato: um pedido de venda foi aprovado (valores congelados). Carrega os itens
    /// para a baixa de estoque (saída por venda) e o valor total, que serve de gatilho
    /// para a geração de obrigações a receber no contexto Financeiro (simétrico à compra).
    /// </summary>
    public sealed class PedidoDeVendaAprovado : EventoDeDominio
    {
        public PedidoDeVendaAprovado(
            Id idPedidoDeVenda,
            Id idCliente,
            decimal valorTotal,
            IReadOnlyCollection<ItemVendaAprovado> itens)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
            IdCliente = idCliente;
            ValorTotal = valorTotal;
            Itens = itens;
        }

        public Id IdPedidoDeVenda { get; }
        public Id IdCliente { get; }
        public decimal ValorTotal { get; }
        public IReadOnlyCollection<ItemVendaAprovado> Itens { get; }
    }

    public sealed record ItemVendaAprovado(long IdProduto, decimal Quantidade);
}
