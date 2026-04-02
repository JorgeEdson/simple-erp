using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Eventos
{
    public class FornecedorReativado : EventoDeDominio
    {
        public FornecedorReativado(Id idAgregadoOrigem) : base(idAgregadoOrigem)
        {
            IdFornecedor = idAgregadoOrigem;
        }

        public Id IdFornecedor { get; }
    }
}
