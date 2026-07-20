using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.UseCases;

namespace simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios
{
    public interface IMovimentacaoDeEstoqueRepository
    {
        Task<Resultado<bool>> AdicionarAsync(MovimentacaoDeEstoque movimentacao, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<MovimentacaoDeEstoque>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarMovimentacoesDeEstoqueFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
