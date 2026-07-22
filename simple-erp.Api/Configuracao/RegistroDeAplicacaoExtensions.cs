using simple_erp.Api.Eventos;
using simple_erp.Api.Mediador;
using simple_erp.Api.Servicos;
using simple_erp.Core.Compartilhado.Eventos;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Api.Configuracao
{   
    public static class RegistroDeAplicacaoExtensions
    {
        public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
        {
            // Serviço de logging do domínio → adaptador sobre o ILogger do ASP.NET.
            services.AddScoped<ILogService, LogService>();

            // Mediator de comandos: o controller depende só do IDispatcher.
            services.AddScoped<IDispatcher, Dispatcher>();

            // Eventos de domínio: o dispatcher entrega cada evento aos seus handlers,
            // que são resolvidos do container dentro do escopo da requisição.
            services.AddScoped<IResolvedorDeManipuladores, ResolvedorDeManipuladoresDoContainer>();
            services.AddScoped<IDispatcherDeEventos, DispatcherDeEventos>();

            // Despacho dos eventos capturados na caixa de saída. Roda fora da requisição:
            // o agregado é confirmado primeiro, os efeitos em outros contextos vêm depois.
            services.AddHostedService<ServicoDeProcessamentoDoOutbox>();

            var assemblyDoCore = typeof(CadastrarClienteUseCase).Assembly;

            RegistrarPorInterfaceGenerica(services, assemblyDoCore, typeof(IUseCase<,>));
            RegistrarPorInterfaceGenerica(services, assemblyDoCore, typeof(IManipuladorDeEventoDeDominio<>));

            return services;
        }
        
        private static void RegistrarPorInterfaceGenerica(
            IServiceCollection services,
            System.Reflection.Assembly assembly,
            Type interfaceGenerica)
        {
            var registros =
                from tipo in assembly.GetTypes()
                where tipo is { IsClass: true, IsAbstract: false }
                from contrato in tipo.GetInterfaces()
                where EhContratoRelevante(contrato, interfaceGenerica)
                select (Servico: contrato, Implementacao: tipo);

            foreach (var (servico, implementacao) in registros)
                services.AddScoped(servico, implementacao);
        }
        
        private static bool EhContratoRelevante(Type contrato, Type interfaceGenerica)
        {
            static bool EhFechamentoDe(Type candidato, Type aberta) =>
                candidato.IsGenericType && candidato.GetGenericTypeDefinition() == aberta;

            return EhFechamentoDe(contrato, interfaceGenerica)
                || contrato.GetInterfaces().Any(herdada => EhFechamentoDe(herdada, interfaceGenerica));
        }
    }
}
