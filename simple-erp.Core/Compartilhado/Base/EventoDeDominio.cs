using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Compartilhado.Base
{
    public abstract class EventoDeDominio
    {
        public Id IdEvento { get; }

        public Id IdAgregadoOrigem { get; }

        public DateTime DataOcorrenciaUtc { get; }

        protected EventoDeDominio(Id idAgregadoOrigem)
        {
            IdEvento = Id.TentarCriar().Instancia;
            IdAgregadoOrigem = idAgregadoOrigem;
            DataOcorrenciaUtc = DateTime.UtcNow;
        }
    }
}
