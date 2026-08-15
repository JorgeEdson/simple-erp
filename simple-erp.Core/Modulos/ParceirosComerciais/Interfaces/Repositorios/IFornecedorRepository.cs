using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios
{
    public interface IFornecedorRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);

        Task<Resultado<Fornecedor>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Resultado<Fornecedor?>> ObterPorDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indica se existe algum fornecedor que satisfaça a especificação. Substitui os antigos
        /// ExistePorDocumento/ExisteOutroPorDocumento: a unicidade no cadastro é
        /// <c>ParceiroComDocumentoSpecification</c>; na edição, essa spec composta com
        /// <c>ParceiroDiferenteDeSpecification</c> via <c>And</c>.
        /// </summary>
        Task<Resultado<bool>> ExisteAsync(ISpecification<Fornecedor> especificacao, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Fornecedor>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarFornecedoresFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
