using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.UseCases;

namespace simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios
{
    public interface IPedidoDeCompraRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(PedidoDeCompra pedidoDeCompra, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(PedidoDeCompra pedidoDeCompra, CancellationToken cancellationToken = default);

        Task<Resultado<PedidoDeCompra>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<PedidoDeCompra>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarPedidosDeCompraFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
