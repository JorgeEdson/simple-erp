using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Suprimentos.Eventos
{
    public sealed class PedidoDeCompraAprovado : EventoDeDominio
    {
        public PedidoDeCompraAprovado(Id idPedidoDeCompra, Id idFornecedor, decimal valorTotal)
            : base(idPedidoDeCompra)
        {
            IdPedidoDeCompra = idPedidoDeCompra;
            IdFornecedor = idFornecedor;
            ValorTotal = valorTotal;
        }

        public Id IdPedidoDeCompra { get; }
        public Id IdFornecedor { get; }

        /// <summary>
        /// Valor total do pedido no momento da aprovação. Serve de gatilho para a
        /// geração de obrigações financeiras a pagar (título) no contexto Financeiro.
        /// </summary>
        public decimal ValorTotal { get; }
    }
}
