using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.Interfaces
{   
    public interface IPossuiEventosDeDominio
    {
        IReadOnlyCollection<EventoDeDominio> EventosDeDominio { get; }

        void LimparEventosDeDominio();
    }
}
