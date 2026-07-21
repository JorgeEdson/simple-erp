using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Estoque
{
    /// <summary>
    /// Repositório do agregado MovimentacaoDeEstoque (extrato append-only). Só oferece
    /// escrita por adição e consulta paginada — movimentações não são editadas nem
    /// removidas. Os filtros do extrato rodam no Postgres, incluindo o tipo de origem
    /// (lido de dentro do jsonb) e o intervalo de datas.
    /// </summary>
    public sealed class MovimentacaoDeEstoqueRepository : IMovimentacaoDeEstoqueRepository
    {
        private const string TabelaComSchema = "estoque.movimentacoes";

        private readonly SimpleErpDbContext _contexto;

        public MovimentacaoDeEstoqueRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            MovimentacaoDeEstoque movimentacao, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<MovimentacaoDeEstoque>().AddAsync(movimentacao, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<MovimentacaoDeEstoque>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarMovimentacoesDeEstoqueFiltros? filtro = null,
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
                    // Extrato: mais recentes primeiro; Id como desempate estável.
                    .OrderByDescending(m => m.DataMovimentacaoUtc)
                    .ThenByDescending(m => m.Id)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<MovimentacaoDeEstoque>>.Sucesso(
                    new ResultadoPaginado<MovimentacaoDeEstoque>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<MovimentacaoDeEstoque>>.Falha(ex.Message);
            }
        }

        private IQueryable<MovimentacaoDeEstoque> CriarConsultaFiltrada(
            ListarMovimentacoesDeEstoqueFiltros? filtro)
        {
            // Filtros opcionais (parâmetro nulo desativa a condição). O tipo de origem
            // é lido de dentro do jsonb: (origem->>'Tipo')::int. As datas delimitam o
            // intervalo [DataInicio, DataFim]. Tipos explícitos garantem os casts com DBNull.
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@id_produto::bigint IS NULL OR id_produto = @id_produto)
                  AND (@tipo::int IS NULL OR tipo = @tipo)
                  AND (@origem_tipo::int IS NULL OR (origem->>'Tipo')::int = @origem_tipo)
                  AND (@data_inicio::timestamptz IS NULL OR data_movimentacao_utc >= @data_inicio)
                  AND (@data_fim::timestamptz IS NULL OR data_movimentacao_utc <= @data_fim)
                """;

            return _contexto.Set<MovimentacaoDeEstoque>().FromSqlRaw(
                sql,
                CriarParametro("id_produto", NpgsqlDbType.Bigint, filtro?.IdProduto),
                CriarParametro("tipo", NpgsqlDbType.Integer, (int?)filtro?.Tipo),
                CriarParametro("origem_tipo", NpgsqlDbType.Integer, (int?)filtro?.OrigemTipo),
                CriarParametro("data_inicio", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataInicio)),
                CriarParametro("data_fim", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataFim)));
        }

        // Tipo explícito no parâmetro: com DBNull o Npgsql não infere o tipo e o cast
        // "::bigint/::int/::timestamptz" do SQL falharia sem isto.
        private static NpgsqlParameter CriarParametro(string nome, NpgsqlDbType tipo, object? valor) =>
            new(nome, tipo) { Value = valor ?? DBNull.Value };

        // O Npgsql exige DateTime com Kind=Utc para colunas timestamptz.
        private static DateTime? NormalizarUtc(DateTime? valor) =>
            valor is null
                ? null
                : DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc);
    }
}
