using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    public sealed class TituloCancelado : EventoDeDominio
    {
        public TituloCancelado(Id idTitulo)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
        }

        public Id IdTitulo { get; }
    }
}
