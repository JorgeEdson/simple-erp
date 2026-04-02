using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Eventos
{
    public sealed class FornecedorCadastrado : EventoDeDominio
    {
        public FornecedorCadastrado(Id idFornecedor, Documento documento, Nome nome)
            : base(idFornecedor)
        {
            IdFornecedor = idFornecedor;
            Documento = documento;
            Nome = nome;
        }

        public Id IdFornecedor { get; }
        public Documento Documento { get; }
        public Nome Nome { get; }
    }
}
