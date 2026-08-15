using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Suprimentos.Eventos
{
    public sealed class PedidoDeCompraCriado : EventoDeDominio
    {
        public PedidoDeCompraCriado(Guid idPedidoDeCompra, Guid idFornecedor)
            : base(idPedidoDeCompra)
        {
            IdPedidoDeCompra = idPedidoDeCompra;
            IdFornecedor = idFornecedor;
        }

        public Guid IdPedidoDeCompra { get; }
        public Guid IdFornecedor { get; }
    }
}
