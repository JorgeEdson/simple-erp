using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;

namespace simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios
{
    public interface ISaldoDeEstoqueRepository
    {
        Task<Resultado<bool>> AdicionarAsync(SaldoDeEstoque saldoDeEstoque, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(SaldoDeEstoque saldoDeEstoque, CancellationToken cancellationToken = default);

        Task<Resultado<SaldoDeEstoque?>> ObterPorProdutoAsync(Id idProduto, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorProdutoAsync(Id idProduto, CancellationToken cancellationToken = default);
    }
}
