using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Eventos
{
    public sealed class FornecedorInativado : EventoDeDominio
    {
        public FornecedorInativado(Guid idFornecedor)
            : base(idFornecedor)
        {
            IdFornecedor = idFornecedor;
        }

        public Guid IdFornecedor { get; }
    }
}
