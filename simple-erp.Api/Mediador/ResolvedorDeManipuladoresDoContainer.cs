using simple_erp.Core.Compartilhado.Interfaces;

namespace simple_erp.Api.Mediador
{   
    public sealed class ResolvedorDeManipuladoresDoContainer : IResolvedorDeManipuladores
    {
        private readonly IServiceProvider _provedor;

        public ResolvedorDeManipuladoresDoContainer(IServiceProvider provedor)
        {
            _provedor = provedor;
        }

        public IReadOnlyCollection<object> ResolverPara(Type tipoDoEvento)
        {
            ArgumentNullException.ThrowIfNull(tipoDoEvento);

            var tipoDoManipulador = typeof(IManipuladorDeEventoDeDominio<>).MakeGenericType(tipoDoEvento);

            return _provedor
                .GetServices(tipoDoManipulador)
                .Where(manipulador => manipulador is not null)
                .Select(manipulador => manipulador!)
                .ToList();
        }
    }
}
