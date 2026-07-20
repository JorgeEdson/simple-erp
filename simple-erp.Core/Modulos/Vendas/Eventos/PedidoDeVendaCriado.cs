using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    public sealed class PedidoDeVendaCriado : EventoDeDominio
    {
        public PedidoDeVendaCriado(Id idPedidoDeVenda, Id idCliente, int numero)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
            IdCliente = idCliente;
            Numero = numero;
        }

        public Id IdPedidoDeVenda { get; }
        public Id IdCliente { get; }
        public int Numero { get; }
    }
}
