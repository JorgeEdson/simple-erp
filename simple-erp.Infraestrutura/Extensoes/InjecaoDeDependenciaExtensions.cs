using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Extensoes
{
    /// <summary>
    /// Ponto único de registro da infraestrutura no container de DI do host
    /// (API, worker, testes de integração). Conforme os módulos ganharem seus
    /// repositórios, eles serão registrados aqui — o host nunca referencia
    /// tipos internos de persistência diretamente.
    /// </summary>
    public static class InjecaoDeDependenciaExtensions
    {
        public static IServiceCollection AdicionarInfraestrutura(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddDbContext<SimpleErpDbContext>(options =>
                options
                    .UseNpgsql(connectionString)
                    // snake_case em tabelas/colunas/índices — convenção do Postgres,
                    // evita identificadores entre aspas ("DataCriacaoUtc" → data_criacao_utc)
                    .UseSnakeCaseNamingConvention());

            // Próximas fases (nesta ordem, uma por vez):
            // 1) UnitOfWork: services.AddScoped<IUnitOfWork, UnitOfWork>();
            // 2) Repositórios por módulo: services.AddScoped<IClienteRepository, ClienteRepository>(); ...
            // 3) Dispatcher/handlers: services.AddScoped<IDispatcherDeEventos, DispatcherDeEventos>(); ...

            return services;
        }
    }
}
