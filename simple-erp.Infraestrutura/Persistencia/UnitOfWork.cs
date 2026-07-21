using Microsoft.EntityFrameworkCore.Storage;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Persistencia
{   
    public sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly SimpleErpDbContext _contexto;
        private IDbContextTransaction? _transacaoAtual;

        public UnitOfWork(
            SimpleErpDbContext contexto,
            IClienteRepository clientesRepository,
            IFornecedorRepository fornecedoresRepository)
        {
            _contexto = contexto;
            ClientesRepository = clientesRepository;
            FornecedoresRepository = fornecedoresRepository;
        }

        // Módulo Parceiros Comerciais — implementado.
        public IClienteRepository ClientesRepository { get; }
        public IFornecedorRepository FornecedoresRepository { get; }

        // Próximos módulos, na ordem do plano incremental.
        public IProdutoRepository ProdutosRepository =>
            throw PendenteDeImplementacao("Catálogo de Produtos", nameof(ProdutosRepository));

        public IPedidoDeCompraRepository PedidosDeCompraRepository =>
            throw PendenteDeImplementacao("Suprimentos", nameof(PedidosDeCompraRepository));

        public ISaldoDeEstoqueRepository SaldosDeEstoqueRepository =>
            throw PendenteDeImplementacao("Estoque", nameof(SaldosDeEstoqueRepository));

        public IMovimentacaoDeEstoqueRepository MovimentacoesDeEstoqueRepository =>
            throw PendenteDeImplementacao("Estoque", nameof(MovimentacoesDeEstoqueRepository));

        public IComposicaoDeProdutoRepository ComposicoesDeProdutoRepository =>
            throw PendenteDeImplementacao("Produção/Composição", nameof(ComposicoesDeProdutoRepository));

        public IOrdemDeProducaoRepository OrdensDeProducaoRepository =>
            throw PendenteDeImplementacao("Produção", nameof(OrdensDeProducaoRepository));

        public IPedidoDeVendaRepository PedidosDeVendaRepository =>
            throw PendenteDeImplementacao("Vendas", nameof(PedidosDeVendaRepository));

        public ITituloRepository TitulosRepository =>
            throw PendenteDeImplementacao("Financeiro", nameof(TitulosRepository));

        public async Task<Resultado<bool>> BeginTransactionAsync(CancellationToken ct = default)
        {
            try
            {
                if (_transacaoAtual is not null)
                    return Resultado<bool>.Falha("TRANSACAO_JA_ABERTA");

                _transacaoAtual = await _contexto.Database.BeginTransactionAsync(ct);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> CommitTransactionAsync(CancellationToken ct = default)
        {
            try
            {
                if (_transacaoAtual is null)
                    return Resultado<bool>.Falha("SEM_TRANSACAO_ATIVA");

                await _transacaoAtual.CommitAsync(ct);
                await _transacaoAtual.DisposeAsync();
                _transacaoAtual = null;

                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> RollbackTransactionAsync(CancellationToken ct = default)
        {
            try
            {
                if (_transacaoAtual is null)
                    return Resultado<bool>.Falha("SEM_TRANSACAO_ATIVA");

                await _transacaoAtual.RollbackAsync(ct);
                await _transacaoAtual.DisposeAsync();
                _transacaoAtual = null;

                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var registrosAfetados = await _contexto.SaveChangesAsync(cancellationToken);
                return Resultado<int>.Sucesso(registrosAfetados);
            }
            catch (Exception ex)
            {
                // A causa raiz (violação de índice único, FK, etc.) costuma estar na inner.
                var mensagem = ex.InnerException?.Message ?? ex.Message;
                return Resultado<int>.Falha(mensagem);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_transacaoAtual is not null)
                await _transacaoAtual.DisposeAsync();
        }

        private static NotImplementedException PendenteDeImplementacao(string modulo, string repositorio) =>
            new($"Repositório '{repositorio}' (módulo {modulo}) ainda não foi implementado na infraestrutura. " +
                "Os módulos estão sendo entregues incrementalmente — Parceiros Comerciais é o primeiro.");
    }
}
