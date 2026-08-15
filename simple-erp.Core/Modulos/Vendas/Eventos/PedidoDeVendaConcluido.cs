using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    public sealed class PedidoDeVendaConcluido : EventoDeDominio
    {
        public PedidoDeVendaConcluido(Guid idPedidoDeVenda)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
        }

        public Guid IdPedidoDeVenda { get; }
    }
}
