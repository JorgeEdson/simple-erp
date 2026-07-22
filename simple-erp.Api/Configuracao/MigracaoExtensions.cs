using Microsoft.EntityFrameworkCore;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Api.Configuracao
{   
    public static class MigracaoExtensions
    {
        public static async Task AplicarMigracoesAsync(
            this WebApplication app,
            int tentativas = 10,
            TimeSpan? intervaloEntreTentativas = null)
        {
            var intervalo = intervaloEntreTentativas ?? TimeSpan.FromSeconds(3);

            using var escopo = app.Services.CreateScope();

            var contexto = escopo.ServiceProvider.GetRequiredService<SimpleErpDbContext>();
            var logger = escopo.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Migracoes");

            for (var tentativa = 1; ; tentativa++)
            {
                try
                {
                    await contexto.Database.MigrateAsync();

                    logger.LogInformation("Migrations aplicadas com sucesso.");
                    return;
                }
                catch (Exception excecao) when (tentativa < tentativas)
                {
                    logger.LogWarning(
                        excecao,
                        "Falha ao aplicar migrations (tentativa {Tentativa}/{Total}). " +
                        "Nova tentativa em {Segundos}s.",
                        tentativa, tentativas, intervalo.TotalSeconds);

                    await Task.Delay(intervalo);
                }
            }
        }
    }
}
