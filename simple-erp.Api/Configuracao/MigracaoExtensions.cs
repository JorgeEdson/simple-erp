using Microsoft.EntityFrameworkCore;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Api.Configuracao
{
    /// <summary>
    /// Aplicação das migrations no startup da API.
    ///
    /// Rodando em containers, a API pode subir antes de o Postgres aceitar conexões —
    /// mesmo com o healthcheck do compose há uma janela de corrida. Por isso a aplicação
    /// das migrations tenta algumas vezes antes de desistir, em vez de derrubar o
    /// processo na primeira falha de conexão.
    /// </summary>
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
