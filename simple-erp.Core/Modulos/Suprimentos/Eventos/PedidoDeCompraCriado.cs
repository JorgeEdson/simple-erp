using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Suprimentos.Eventos
{
    public sealed class PedidoDeCompraCriado : EventoDeDominio
    {
        public PedidoDeCompraCriado(Id idPedidoDeCompra, Id idFornecedor)
            : base(idPedidoDeCompra)
        {
            IdPedidoDeCompra = idPedidoDeCompra;
            IdFornecedor = idFornecedor;
        }

        public Id IdPedidoDeCompra { get; }
        public Id IdFornecedor { get; }
    }
}
