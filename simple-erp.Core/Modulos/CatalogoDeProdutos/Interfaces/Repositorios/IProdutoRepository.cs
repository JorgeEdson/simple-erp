using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios
{
    public interface IProdutoRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(Produto produto, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Produto produto, CancellationToken cancellationToken = default);

        Task<Resultado<Produto>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Resultado<Produto?>> ObterPorCodigoAsync(CodigoProduto codigo, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> ExistePorCodigoAsync(CodigoProduto codigo, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> ExisteOutroPorCodigoAsync(Guid idProduto, CodigoProduto codigo, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Produto>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarProdutosFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
