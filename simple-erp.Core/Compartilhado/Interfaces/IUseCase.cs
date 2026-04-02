using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    public interface IUseCase<in TEntrada, TSaida>
    {
        Task<Resultado<TSaida>> ExecutarAsync(TEntrada dados, CancellationToken cancellationToken = default);
    }
}
