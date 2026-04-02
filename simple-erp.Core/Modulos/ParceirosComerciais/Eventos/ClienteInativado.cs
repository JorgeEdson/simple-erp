using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Eventos
{
    public sealed class ClienteInativado : EventoDeDominio
    {
        public ClienteInativado(Id idCliente)
            : base(idCliente)
        {
            IdCliente = idCliente;
        }

        public Id IdCliente { get; }
    }
}
