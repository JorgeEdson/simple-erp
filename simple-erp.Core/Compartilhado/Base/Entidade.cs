using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Compartilhado.Base
{
    public abstract class Entidade<TEntidade> : IPossuiEventosDeDominio
    {
        private readonly List<EventoDeDominio> _eventosDeDominio = new();
        protected Entidade(
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
        {
            Id = Id.TentarCriar(id).Instancia;
            DataCriacaoUtc = dataCriacaoUtc ?? DateTime.UtcNow;
            DataAtualizacaoUtc = dataAtualizacaoUtc ?? DateTime.UtcNow;            
        }

        public Id Id { get; protected set; }
        public DateTime DataCriacaoUtc { get; protected set; }
        public DateTime DataAtualizacaoUtc { get; protected set; }
        public IReadOnlyCollection<EventoDeDominio> EventosDeDominio =>
           _eventosDeDominio.AsReadOnly();

        public bool IgualA(Entidade<TEntidade> entidade)
        {
            return Id.IgualA(entidade.Id);
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
            return $"{typeof(TEntidade).Name} [{Id.Valor}]";
        }
    }
}
