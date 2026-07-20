using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Suprimentos.Eventos
{
    public sealed class PedidoDeCompraCancelado : EventoDeDominio
    {
        public PedidoDeCompraCancelado(Id idPedidoDeCompra)
            : base(idPedidoDeCompra)
        {
            IdPedidoDeCompra = idPedidoDeCompra;
        }

        public Id IdPedidoDeCompra { get; }
    }
}
