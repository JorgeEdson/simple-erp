using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.UseCases;

namespace simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios
{
    public interface ITituloRepository
    {
        Task<Resultado<bool>> AdicionarAsync(Titulo titulo, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Titulo titulo, CancellationToken cancellationToken = default);

        Task<Resultado<Titulo?>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Titulo>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarTitulosFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
