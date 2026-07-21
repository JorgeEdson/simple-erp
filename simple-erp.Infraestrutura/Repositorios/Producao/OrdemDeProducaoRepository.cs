using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Producao
{
    /// <summary>
    /// Repositório do agregado OrdemDeProducao. Contratos usuais: sem SaveChanges,
    /// ObterPorId rastreado (caminho de confirmar/concluir/cancelar), "não encontrado"
    /// é Sucesso com nulo, e falhas de infra viram Resultado.Falha. As necessidades
    /// viajam junto no jsonb da ordem.
    /// </summary>
    public sealed class OrdemDeProducaoRepository : IOrdemDeProducaoRepository
    {
        private const string TabelaComSchema = "producao.ordens_de_producao";

        private readonly SimpleErpDbContext _contexto;

        public OrdemDeProducaoRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            OrdemDeProducao ordem, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<OrdemDeProducao>().AddAsync(ordem, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            OrdemDeProducao ordem, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(ordem).State == EntityState.Detached)
                    _contexto.Set<OrdemDeProducao>().Update(ordem);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<OrdemDeProducao?>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: caminho de escrita (confirmar / concluir / cancelar → atualizar).
                var ordem = await _contexto.Set<OrdemDeProducao>()
                    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

                return Resultado<OrdemDeProducao?>.Sucesso(ordem);
            }
            catch (Exception ex)
            {
                return Resultado<OrdemDeProducao?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<OrdemDeProducao>()
                    .AsNoTracking()
                    .AnyAsync(o => o.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<OrdemDeProducao>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarOrdensDeProducaoFiltros? filtro = null,
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
                    .OrderByDescending(o => o.DataCriacaoUtc)
                    .ThenByDescending(o => o.Id)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<OrdemDeProducao>>.Sucesso(
                    new ResultadoPaginado<OrdemDeProducao>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<OrdemDeProducao>>.Falha(ex.Message);
            }
        }

        private IQueryable<OrdemDeProducao> CriarConsultaFiltrada(ListarOrdensDeProducaoFiltros? filtro)
        {
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@id_produto::bigint IS NULL OR id_produto_fabricado = @id_produto)
                  AND (@status::int IS NULL OR status = @status)
                  AND (@data_inicio::timestamptz IS NULL OR data_criacao_utc >= @data_inicio)
                  AND (@data_fim::timestamptz IS NULL OR data_criacao_utc <= @data_fim)
                """;

            return _contexto.Set<OrdemDeProducao>().FromSqlRaw(
                sql,
                CriarParametro("id_produto", NpgsqlDbType.Bigint, filtro?.IdProdutoFabricado),
                CriarParametro("status", NpgsqlDbType.Integer, (int?)filtro?.Status),
                CriarParametro("data_inicio", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataInicio)),
                CriarParametro("data_fim", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataFim)));
        }

        private static NpgsqlParameter CriarParametro(string nome, NpgsqlDbType tipo, object? valor) =>
            new(nome, tipo) { Value = valor ?? DBNull.Value };

        private static DateTime? NormalizarUtc(DateTime? valor) =>
            valor is null ? null : DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc);
    }
}
