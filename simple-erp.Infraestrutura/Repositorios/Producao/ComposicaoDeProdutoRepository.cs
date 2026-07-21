using Microsoft.EntityFrameworkCore;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Producao
{
    /// <summary>
    /// Repositório do agregado ComposicaoDeProduto (subdomínio Composição). Além das
    /// operações usuais, oferece consultas próprias da receita: existência e obtenção
    /// da versão ativa por produto, e o próximo número de versão (para versionamento).
    ///
    /// As consultas usam LINQ (colunas escalares, sem filtro em jsonb). ObterPorId e
    /// ObterAtivaPorProduto retornam entidades RASTREADAS — são caminhos de escrita
    /// (ativar/inativar). Não há SaveChanges aqui.
    /// </summary>
    public sealed class ComposicaoDeProdutoRepository : IComposicaoDeProdutoRepository
    {
        private readonly SimpleErpDbContext _contexto;

        public ComposicaoDeProdutoRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            ComposicaoDeProduto composicao, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<ComposicaoDeProduto>().AddAsync(composicao, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            ComposicaoDeProduto composicao, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(composicao).State == EntityState.Detached)
                    _contexto.Set<ComposicaoDeProduto>().Update(composicao);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<ComposicaoDeProduto?>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: caminho de escrita (ativar / inativar → atualizar).
                var composicao = await _contexto.Set<ComposicaoDeProduto>()
                    .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

                return Resultado<ComposicaoDeProduto?>.Sucesso(composicao);
            }
            catch (Exception ex)
            {
                return Resultado<ComposicaoDeProduto?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExisteAtivaPorProdutoAsync(
            Id idProdutoFabricado, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<ComposicaoDeProduto>()
                    .AsNoTracking()
                    .AnyAsync(c => c.IdProdutoFabricado == idProdutoFabricado && c.Ativa, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ComposicaoDeProduto?>> ObterAtivaPorProdutoAsync(
            Id idProdutoFabricado, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: é usado no fluxo de ativação para inativar a versão vigente.
                var ativa = await _contexto.Set<ComposicaoDeProduto>()
                    .FirstOrDefaultAsync(c => c.IdProdutoFabricado == idProdutoFabricado && c.Ativa, cancellationToken);

                return Resultado<ComposicaoDeProduto?>.Sucesso(ativa);
            }
            catch (Exception ex)
            {
                return Resultado<ComposicaoDeProduto?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<int>> ObterProximaVersaoPorProdutoAsync(
            Id idProdutoFabricado, CancellationToken cancellationToken = default)
        {
            try
            {
                // Sem versões ainda → começa em 1; caso contrário, maior versão + 1.
                var maiorVersao = await _contexto.Set<ComposicaoDeProduto>()
                    .AsNoTracking()
                    .Where(c => c.IdProdutoFabricado == idProdutoFabricado)
                    .Select(c => (int?)c.Versao)
                    .MaxAsync(cancellationToken);

                return Resultado<int>.Sucesso((maiorVersao ?? 0) + 1);
            }
            catch (Exception ex)
            {
                return Resultado<int>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<ComposicaoDeProduto>>> ListarPorProdutoPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarVersoesDeComposicaoFiltros filtro,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pagina = numeroPagina < 1 ? 1 : numeroPagina;
                var tamanho = tamanhoPagina < 1 ? 10 : Math.Min(tamanhoPagina, 100);

                var idProduto = Id.TentarCriar(filtro.IdProdutoFabricado).Instancia;

                var consulta = _contexto.Set<ComposicaoDeProduto>()
                    .AsNoTracking()
                    .Where(c => c.IdProdutoFabricado == idProduto);

                if (filtro.ApenasAtivas == true)
                    consulta = consulta.Where(c => c.Ativa);

                var total = await consulta.CountAsync(cancellationToken);

                // Versões em ordem decrescente: a mais recente primeiro.
                var itens = await consulta
                    .OrderByDescending(c => c.Versao)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<ComposicaoDeProduto>>.Sucesso(
                    new ResultadoPaginado<ComposicaoDeProduto>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<ComposicaoDeProduto>>.Falha(ex.Message);
            }
        }
    }
}
