using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    public interface IUnitOfWork
    {
        IClienteRepository ClientesRepository { get; }
        IFornecedorRepository FornecedoresRepository { get; }
        IProdutoRepository ProdutosRepository { get; }
        IPedidoDeCompraRepository PedidosDeCompraRepository { get; }
        ISaldoDeEstoqueRepository SaldosDeEstoqueRepository { get; }
        IMovimentacaoDeEstoqueRepository MovimentacoesDeEstoqueRepository { get; }
        IComposicaoDeProdutoRepository ComposicoesDeProdutoRepository { get; }
        IOrdemDeProducaoRepository OrdensDeProducaoRepository { get; }
        IPedidoDeVendaRepository PedidosDeVendaRepository { get; }
        ITituloRepository TitulosRepository { get; }

        Task<Resultado<bool>> BeginTransactionAsync(CancellationToken ct = default);
        Task<Resultado<bool>> CommitTransactionAsync(CancellationToken ct = default);
        Task<Resultado<bool>> RollbackTransactionAsync(CancellationToken ct = default);

        Task<Resultado<int>> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
