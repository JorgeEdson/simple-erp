using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Vendas.Eventos
{
    public sealed class PedidoDeVendaCriado : EventoDeDominio
    {
        public PedidoDeVendaCriado(Guid idPedidoDeVenda, Guid idCliente, int numero)
            : base(idPedidoDeVenda)
        {
            IdPedidoDeVenda = idPedidoDeVenda;
            IdCliente = idCliente;
            Numero = numero;
        }

        public Guid IdPedidoDeVenda { get; }
        public Guid IdCliente { get; }
        public int Numero { get; }
    }
}
