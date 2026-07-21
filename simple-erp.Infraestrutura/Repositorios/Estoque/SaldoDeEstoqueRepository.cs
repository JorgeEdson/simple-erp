using Microsoft.EntityFrameworkCore;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Estoque
{
    /// <summary>
    /// Repositório do agregado SaldoDeEstoque. ObterPorProdutoAsync retorna a entidade
    /// RASTREADA — é o caminho de escrita: o use case obtém o saldo, chama Movimentar
    /// e depois Atualiza, tudo na mesma unidade de trabalho. Não chama SaveChanges.
    /// </summary>
    public sealed class SaldoDeEstoqueRepository : ISaldoDeEstoqueRepository
    {
        private readonly SimpleErpDbContext _contexto;

        public SaldoDeEstoqueRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            SaldoDeEstoque saldoDeEstoque, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<SaldoDeEstoque>().AddAsync(saldoDeEstoque, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            SaldoDeEstoque saldoDeEstoque, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(saldoDeEstoque).State == EntityState.Detached)
                    _contexto.Set<SaldoDeEstoque>().Update(saldoDeEstoque);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<SaldoDeEstoque?>> ObterPorProdutoAsync(
            Id idProduto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: este é o caminho de escrita (movimentar + atualizar).
                var saldo = await _contexto.Set<SaldoDeEstoque>()
                    .FirstOrDefaultAsync(s => s.IdProduto == idProduto, cancellationToken);

                return Resultado<SaldoDeEstoque?>.Sucesso(saldo);
            }
            catch (Exception ex)
            {
                return Resultado<SaldoDeEstoque?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorProdutoAsync(
            Id idProduto, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<SaldoDeEstoque>()
                    .AsNoTracking()
                    .AnyAsync(s => s.IdProduto == idProduto, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }
    }
}
