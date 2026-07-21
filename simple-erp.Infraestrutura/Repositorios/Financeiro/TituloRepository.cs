using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Financeiro
{
    /// <summary>
    /// Repositório do agregado Titulo. Segue os contratos dos demais módulos:
    /// - nenhum SaveChanges (a transação pertence ao UnitOfWork);
    /// - ObterPorIdAsync retorna a entidade RASTREADA (caminho de baixa/cancelamento);
    /// - "não encontrado" é Sucesso com instância nula, não falha de infraestrutura;
    /// - falhas de infra viram Resultado.Falha.
    ///
    /// O histórico de baixas viaja junto no jsonb do título — carregar o título já
    /// traz suas baixas, sem consulta adicional.
    /// </summary>
    public sealed class TituloRepository : ITituloRepository
    {
        private const string TabelaComSchema = "financeiro.titulos";

        private readonly SimpleErpDbContext _contexto;

        public TituloRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            Titulo titulo, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<Titulo>().AddAsync(titulo, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            Titulo titulo, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(titulo).State == EntityState.Detached)
                    _contexto.Set<Titulo>().Update(titulo);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<Titulo?>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: caminho de escrita (baixar / cancelar → atualizar).
                var titulo = await _contexto.Set<Titulo>()
                    .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

                return Resultado<Titulo?>.Sucesso(titulo);
            }
            catch (Exception ex)
            {
                return Resultado<Titulo?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<Titulo>()
                    .AsNoTracking()
                    .AnyAsync(t => t.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<Titulo>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarTitulosFiltros? filtro = null,
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
                    // Extrato por vencimento (mais próximos primeiro); Id como desempate.
                    .OrderBy(t => t.DataVencimentoUtc)
                    .ThenBy(t => t.Id)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<Titulo>>.Sucesso(
                    new ResultadoPaginado<Titulo>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<Titulo>>.Falha(ex.Message);
            }
        }

        private IQueryable<Titulo> CriarConsultaFiltrada(ListarTitulosFiltros? filtro)
        {
            // Filtros opcionais (parâmetro nulo desativa a condição). Tipos explícitos
            // garantem os casts com DBNull; as datas delimitam o intervalo de vencimento.
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@tipo::int IS NULL OR tipo = @tipo)
                  AND (@status::int IS NULL OR status = @status)
                  AND (@id_parceiro::bigint IS NULL OR id_parceiro = @id_parceiro)
                  AND (@venc_inicio::timestamptz IS NULL OR data_vencimento_utc >= @venc_inicio)
                  AND (@venc_fim::timestamptz IS NULL OR data_vencimento_utc <= @venc_fim)
                """;

            return _contexto.Set<Titulo>().FromSqlRaw(
                sql,
                CriarParametro("tipo", NpgsqlDbType.Integer, (int?)filtro?.Tipo),
                CriarParametro("status", NpgsqlDbType.Integer, (int?)filtro?.Status),
                CriarParametro("id_parceiro", NpgsqlDbType.Bigint, filtro?.IdParceiro),
                CriarParametro("venc_inicio", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.VencimentoInicio)),
                CriarParametro("venc_fim", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.VencimentoFim)));
        }

        private static NpgsqlParameter CriarParametro(string nome, NpgsqlDbType tipo, object? valor) =>
            new(nome, tipo) { Value = valor ?? DBNull.Value };

        private static DateTime? NormalizarUtc(DateTime? valor) =>
            valor is null
                ? null
                : DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc);
    }
}
