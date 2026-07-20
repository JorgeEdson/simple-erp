using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    public sealed class PedidoDeVendaCancelado : EventoDeDominio
    {
        public PedidoDeVendaCancelado(Id idPedidoDeVenda, string motivo)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
            Motivo = motivo;
        }

        public Id IdPedidoDeVenda { get; }
        public string Motivo { get; }
    }
}
