using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Infraestrutura.Persistencia.Outbox;

namespace simple_erp.Infraestrutura.Persistencia.Interceptadores
{   
    public sealed class CapturaDeEventosParaOutboxInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData dadosDoEvento, InterceptionResult<int> resultado)
        {
            CapturarEventos(dadosDoEvento.Context);
            return base.SavingChanges(dadosDoEvento, resultado);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData dadosDoEvento,
            InterceptionResult<int> resultado,
            CancellationToken cancellationToken = default)
        {
            CapturarEventos(dadosDoEvento.Context);
            return base.SavingChangesAsync(dadosDoEvento, resultado, cancellationToken);
        }

        private static void CapturarEventos(DbContext? contexto)
        {
            if (contexto is null)
                return;

            var agregadosComEventos = contexto.ChangeTracker
                .Entries<IPossuiEventosDeDominio>()
                .Select(entrada => entrada.Entity)
                .Where(agregado => agregado.EventosDeDominio.Count > 0)
                .ToList();

            if (agregadosComEventos.Count == 0)
                return;

            foreach (var agregado in agregadosComEventos)
            {   
                var eventos = agregado.EventosDeDominio.ToList();

                foreach (var evento in eventos)
                    contexto.Set<EventoNoOutbox>().Add(EventoNoOutbox.APartirDe(evento));

                agregado.LimparEventosDeDominio();
            }
        }
    }
}
