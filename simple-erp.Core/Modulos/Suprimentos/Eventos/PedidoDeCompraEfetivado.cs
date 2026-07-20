using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Suprimentos.Eventos
{
    /// <summary>
    /// Fato de negócio: um pedido de compra aprovado foi efetivado (entrada concluída).
    /// É o evento de integração consumido pelo contexto de Estoque para gerar a
    /// entrada por compra (movimentação de estoque) e, conforme a regra adotada,
    /// pelo contexto Financeiro para gerar o título a pagar.
    /// </summary>
    public sealed class PedidoDeCompraEfetivado : EventoDeDominio
    {
        public PedidoDeCompraEfetivado(
            Id idPedidoDeCompra,
            Id idFornecedor,
            decimal valorTotal,
            IReadOnlyCollection<ItemPedidoDeCompraEfetivado> itens)
            : base(idPedidoDeCompra)
        {
            IdPedidoDeCompra = idPedidoDeCompra;
            IdFornecedor = idFornecedor;
            ValorTotal = valorTotal;
            Itens = itens;
        }

        public Id IdPedidoDeCompra { get; }
        public Id IdFornecedor { get; }
        public decimal ValorTotal { get; }
        public IReadOnlyCollection<ItemPedidoDeCompraEfetivado> Itens { get; }
    }

    public sealed record ItemPedidoDeCompraEfetivado(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);
}
