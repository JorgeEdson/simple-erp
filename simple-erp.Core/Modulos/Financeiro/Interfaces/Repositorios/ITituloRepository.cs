using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.UseCases;

namespace simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios
{
    public interface ITituloRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(Titulo titulo, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Titulo titulo, CancellationToken cancellationToken = default);

        Task<Resultado<Titulo?>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Titulo>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarTitulosFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
