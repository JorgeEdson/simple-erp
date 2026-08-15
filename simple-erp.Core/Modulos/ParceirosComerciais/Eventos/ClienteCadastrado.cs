using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Eventos
{
    public sealed class ClienteCadastrado : EventoDeDominio
    {
        public ClienteCadastrado(
            Guid idCliente,
            Documento documento,
            Nome nome)
            : base(idCliente)
        {
            IdCliente = idCliente;
            Documento = documento;
            Nome = nome;
        }

        public Guid IdCliente { get; }
        public Documento Documento { get; }
        public Nome Nome { get; }
    }
}
