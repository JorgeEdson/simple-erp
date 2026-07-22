using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.UseCases;

namespace simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios
{
    public interface IPedidoDeVendaRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(PedidoDeVenda pedidoDeVenda, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(PedidoDeVenda pedidoDeVenda, CancellationToken cancellationToken = default);

        Task<Resultado<PedidoDeVenda?>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);

        /// <summary>Retorna o próximo número sequencial de pedido de venda.</summary>
        Task<Resultado<int>> ObterProximoNumeroAsync(CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<PedidoDeVenda>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarPedidosDeVendaFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
