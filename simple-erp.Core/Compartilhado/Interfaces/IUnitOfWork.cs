using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    public interface IUnitOfWork
    {
        IClienteRepository ClientesRepository { get; }
        IFornecedorRepository FornecedoresRepository { get; }

        Task<Resultado<bool>> BeginTransactionAsync(CancellationToken ct = default);
        Task<Resultado<bool>> CommitTransactionAsync(CancellationToken ct = default);
        Task<Resultado<bool>> RollbackTransactionAsync(CancellationToken ct = default);

        Task<Resultado<int>> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
