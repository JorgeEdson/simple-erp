using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.CatalogoDeProdutos
{   
    public sealed class ProdutoRepository : IProdutoRepository
    {
        private const string TabelaComSchema = "catalogo.produtos";

        private readonly SimpleErpDbContext _contexto;

        public ProdutoRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            Produto produto, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<Produto>().AddAsync(produto, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            Produto produto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(produto).State == EntityState.Detached)
                    _contexto.Set<Produto>().Update(produto);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<Produto>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var produto = await _contexto.Set<Produto>()
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                // Contrato: Sucesso com instância nula quando não encontrado —
                // o use case decide o erro de negócio (ex.: PRODUTO_NAO_ENCONTRADO).
                return Resultado<Produto>.Sucesso(produto!);
            }
            catch (Exception ex)
            {
                return Resultado<Produto>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<Produto?>> ObterPorCodigoAsync(
            CodigoProduto codigo, CancellationToken cancellationToken = default)
        {
            try
            {
                var produto = await _contexto.Set<Produto>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Codigo == codigo, cancellationToken);

                return Resultado<Produto?>.Sucesso(produto);
            }
            catch (Exception ex)
            {
                return Resultado<Produto?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<Produto>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorCodigoAsync(
            CodigoProduto codigo, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<Produto>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Codigo == codigo, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExisteOutroPorCodigoAsync(
            Id idProduto, CodigoProduto codigo, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<Produto>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Codigo == codigo && p.Id != idProduto, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<Produto>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarProdutosFiltros? filtro = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var pagina = numeroPagina < 1 ? 1 : numeroPagina;
                var tamanho = tamanhoPagina < 1 ? 10 : Math.Min(tamanhoPagina, 100);

                var consulta = CriarConsultaFiltrada(filtro);

                var total = await consulta.CountAsync(cancellationToken);

                var itens = await consulta
                    .AsNoTracking()
                    .OrderBy(p => p.Codigo)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<Produto>>.Sucesso(
                    new ResultadoPaginado<Produto>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<Produto>>.Falha(ex.Message);
            }
        }

        private IQueryable<Produto> CriarConsultaFiltrada(ListarProdutosFiltros? filtro)
        {
            // Todos os filtros são opcionais: parâmetro nulo desativa a condição.
            // SQL parametrizado e composable — o EF o envolve para o Count e a paginação.
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@codigo::text IS NULL OR codigo ILIKE @codigo)
                  AND (@descricao::text IS NULL OR descricao ILIKE @descricao)
                  AND (@unidade::text IS NULL OR unidade_de_medida = @unidade)
                  AND (@classificacao::int IS NULL OR classificacao = @classificacao)
                  AND (@ativo::boolean IS NULL OR ativo = @ativo)
                """;

            return _contexto.Set<Produto>().FromSqlRaw(
                sql,
                CriarParametro("codigo", NpgsqlDbType.Text, PadraoContem(NormalizarCodigo(filtro?.Codigo))),
                CriarParametro("descricao", NpgsqlDbType.Text, PadraoContem(filtro?.Descricao)),
                CriarParametro("unidade", NpgsqlDbType.Text, NormalizarUnidade(filtro?.UnidadeDeMedida)),
                CriarParametro("classificacao", NpgsqlDbType.Integer, (int?)filtro?.Classificacao),
                CriarParametro("ativo", NpgsqlDbType.Boolean, filtro?.Ativo));
        }

        // Tipo explícito no parâmetro: quando o valor é DBNull, o Npgsql não consegue
        // inferir o tipo e o cast "::text/::int/::boolean" do SQL falharia sem isto.
        private static NpgsqlParameter CriarParametro(string nome, NpgsqlDbType tipo, object? valor) =>
            new(nome, tipo) { Value = valor ?? DBNull.Value };

        private static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static string? NormalizarCodigo(string? valor) =>
            Normalizar(valor)?.ToUpperInvariant();

        private static string? NormalizarUnidade(string? valor) =>
            Normalizar(valor)?.ToUpperInvariant();

        private static string? PadraoContem(string? valor)
        {
            var normalizado = Normalizar(valor);
            return normalizado is null ? null : $"%{normalizado}%";
        }
    }
}
