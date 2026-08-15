using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Compartilhado.Base
{
    public abstract class Entidade<TEntidade> : IPossuiEventosDeDominio
    {
        private readonly List<EventoDeDominio> _eventosDeDominio = new();
        protected Entidade(
            Guid? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
        {
            Id = id ?? Guid.NewGuid();
            DataCriacaoUtc = dataCriacaoUtc ?? DateTime.UtcNow;
            DataAtualizacaoUtc = dataAtualizacaoUtc ?? DateTime.UtcNow;
        }

        public Guid Id { get; protected set; }
        public DateTime DataCriacaoUtc { get; protected set; }
        public DateTime DataAtualizacaoUtc { get; protected set; }
        public IReadOnlyCollection<EventoDeDominio> EventosDeDominio =>
           _eventosDeDominio.AsReadOnly();

        public bool IgualA(Entidade<TEntidade> entidade)
        {
            return entidade is not null && Id == entidade.Id;
        }

        public bool DiferenteDe(Entidade<TEntidade> entidade)
        {
            return !IgualA(entidade);
        }

        protected void AtualizarDataAtualizacao()
        {
            DataAtualizacaoUtc = DateTime.UtcNow;
        }

        protected void AdicionarEventoDeDominio(EventoDeDominio eventoDeDominio)
        {
            if (eventoDeDominio is null)
                return;

            _eventosDeDominio.Add(eventoDeDominio);
        }

        protected void AdicionarEventosDeDominio(IEnumerable<EventoDeDominio> eventosDeDominio)
        {
            if (eventosDeDominio is null)
                return;

            foreach (var eventoDeDominio in eventosDeDominio)
            {
                if (eventoDeDominio is not null)
                    _eventosDeDominio.Add(eventoDeDominio);
            }
        }

        public void LimparEventosDeDominio()
        {
            _eventosDeDominio.Clear();
        }

        public override string ToString()
        {
            return $"{typeof(TEntidade).Name} [{Id}]";
        }
    }
}
