using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    public sealed class OrdemDeProducaoCancelada : EventoDeDominio
    {
        public OrdemDeProducaoCancelada(Id idOrdemDeProducao)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
        }

        public Id IdOrdemDeProducao { get; }
    }
}
