using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Infraestrutura.Persistencia;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using simple_erp.Infraestrutura.Persistencia.Interceptadores;
using simple_erp.Infraestrutura.Persistencia.Outbox;
using System.Reflection;

namespace simple_erp.Infraestrutura.Extensoes
{
    public static class InjecaoDeDependenciaExtensions
    {
        private static readonly Assembly AssemblyDaInfraestrutura =
            typeof(InjecaoDeDependenciaExtensions).Assembly;

        public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SimpleErpDbContext>(options =>
                options
                    .UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(new CapturaDeEventosParaOutboxInterceptor()));

            
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AdicionarRepositorios();

            // Singleton porque não guarda estado por requisição: ele abre o próprio
            // escopo para cada evento que processa. Registrá-lo como Scoped criaria uma
            // dependência cativa quando um worker singleton o consumisse.
            services.AddSingleton<IProcessadorDeEventosPendentes, ProcessadorDeEventosPendentes>();

            return services;
        }

        
        public static IServiceCollection AdicionarRepositorios(this IServiceCollection services)
        {
            foreach (var (contrato, implementacao) in DescobrirRepositorios())
                services.AddScoped(contrato, implementacao);

            return services;
        }

        private static IEnumerable<(Type Contrato, Type Implementacao)> DescobrirRepositorios() =>
            AssemblyDaInfraestrutura
                .GetTypes()
                .Where(tipo => tipo is { IsClass: true, IsAbstract: false })                
                .SelectMany(tipo => tipo
                    .GetInterfaces()
                    .Where(EhContratoDeRepositorio)
                    .Select(contrato => (Contrato: contrato, Implementacao: tipo)));

        
        private static bool EhContratoDeRepositorio(Type contrato) =>
            contrato != typeof(IRepositorio) && typeof(IRepositorio).IsAssignableFrom(contrato);
    }
}
