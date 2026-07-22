using simple_erp.Api.Mediador;
using simple_erp.Api.Servicos;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Api.Configuracao
{   
    public static class RegistroDeAplicacaoExtensions
    {
        private const string NamespaceParceiros = "simple_erp.Core.Modulos.ParceirosComerciais";

        public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
        {
            // Serviço de logging do domínio → adaptador sobre o ILogger do ASP.NET.
            services.AddScoped<ILogService, LogService>();

            // Mediator: o controller depende só do IDispatcher; ele resolve o use case.
            services.AddScoped<IDispatcher, Dispatcher>();

            RegistrarUseCasesPorScanning(services, NamespaceParceiros);

            return services;
        }

        
        private static void RegistrarUseCasesPorScanning(IServiceCollection services, string prefixoNamespace)
        {   
            var assemblyDoCore = typeof(CadastrarClienteUseCase).Assembly;

            var registros =
                from tipo in assemblyDoCore.GetTypes()
                where tipo is { IsClass: true, IsAbstract: false }
                where tipo.Namespace is not null
                      && tipo.Namespace.StartsWith(prefixoNamespace, StringComparison.Ordinal)
                from interfaceUseCase in tipo.GetInterfaces()
                where interfaceUseCase.IsGenericType
                      && interfaceUseCase.GetGenericTypeDefinition() == typeof(IUseCase<,>)
                select (Servico: interfaceUseCase, Implementacao: tipo);

            foreach (var (servico, implementacao) in registros)
                services.AddScoped(servico, implementacao);
        }
    }
}
