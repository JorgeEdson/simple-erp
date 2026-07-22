using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.UseCases;

namespace simple_erp.Core.Modulos.Producao.Interfaces.Repositorios
{
    public interface IOrdemDeProducaoRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(OrdemDeProducao ordem, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(OrdemDeProducao ordem, CancellationToken cancellationToken = default);

        Task<Resultado<OrdemDeProducao?>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<OrdemDeProducao>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarOrdensDeProducaoFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
