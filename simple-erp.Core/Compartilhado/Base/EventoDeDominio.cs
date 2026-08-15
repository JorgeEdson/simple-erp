namespace simple_erp.Core.Compartilhado.Base
{
    public abstract class EventoDeDominio
    {
        public Guid IdEvento { get; }

        public Guid IdAgregadoOrigem { get; }

        public DateTime DataOcorrenciaUtc { get; }

        protected EventoDeDominio(Guid idAgregadoOrigem)
        {
            IdEvento = Guid.NewGuid();
            IdAgregadoOrigem = idAgregadoOrigem;
            DataOcorrenciaUtc = DateTime.UtcNow;
        }
    }
}
