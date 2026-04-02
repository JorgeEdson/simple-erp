using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios
{
    public interface IFornecedorRepository
    {
        Task<Resultado<bool>> AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);

        Task<Resultado<Fornecedor>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);
        Task<Resultado<Fornecedor?>> ObterPorDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> ExistePorDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> ExisteOutroPorDocumentoAsync(Id idFornecedor, Documento documento, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Fornecedor>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarFornecedoresFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
