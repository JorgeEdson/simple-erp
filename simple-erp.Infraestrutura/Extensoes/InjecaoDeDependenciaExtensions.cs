using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Infraestrutura.Persistencia;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using simple_erp.Infraestrutura.Repositorios.ParceirosComerciais;

namespace simple_erp.Infraestrutura.Extensoes
{   
    public static class InjecaoDeDependenciaExtensions
    {
        public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SimpleErpDbContext>(options =>
                options
                    .UseNpgsql(connectionString)                    
                    .UseSnakeCaseNamingConvention());
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Módulo Parceiros Comerciais.
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IFornecedorRepository, FornecedorRepository>();

            return services;
        }
    }
}
