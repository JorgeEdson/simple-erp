using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    public sealed class PedidoDeVendaConcluido : EventoDeDominio
    {
        public PedidoDeVendaConcluido(Id idPedidoDeVenda)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
        }

        public Id IdPedidoDeVenda { get; }
    }
}
