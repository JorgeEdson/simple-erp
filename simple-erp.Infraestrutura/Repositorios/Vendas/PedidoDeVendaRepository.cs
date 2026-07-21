using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Vendas
{
    /// <summary>
    /// Repositório do agregado PedidoDeVenda. Contratos usuais: sem SaveChanges,
    /// ObterPorId rastreado (caminho de editar/aprovar/concluir/cancelar), "não
    /// encontrado" é Sucesso com nulo, e falhas de infra viram Resultado.Falha. Os
    /// itens viajam junto no jsonb do pedido. Oferece ainda o próximo número sequencial.
    /// </summary>
    public sealed class PedidoDeVendaRepository : IPedidoDeVendaRepository
    {
        private const string TabelaComSchema = "vendas.pedidos_de_venda";

        private readonly SimpleErpDbContext _contexto;

        public PedidoDeVendaRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            PedidoDeVenda pedidoDeVenda, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<PedidoDeVenda>().AddAsync(pedidoDeVenda, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            PedidoDeVenda pedidoDeVenda, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(pedidoDeVenda).State == EntityState.Detached)
                    _contexto.Set<PedidoDeVenda>().Update(pedidoDeVenda);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<PedidoDeVenda?>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: caminho de escrita (editar / aprovar / concluir / cancelar).
                var pedido = await _contexto.Set<PedidoDeVenda>()
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                return Resultado<PedidoDeVenda?>.Sucesso(pedido);
            }
            catch (Exception ex)
            {
                return Resultado<PedidoDeVenda?>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<PedidoDeVenda>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<int>> ObterProximoNumeroAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Sem pedidos → começa em 1; caso contrário, maior número + 1.
                var maiorNumero = await _contexto.Set<PedidoDeVenda>()
                    .AsNoTracking()
                    .Select(p => (int?)p.Numero)
                    .MaxAsync(cancellationToken);

                return Resultado<int>.Sucesso((maiorNumero ?? 0) + 1);
            }
            catch (Exception ex)
            {
                return Resultado<int>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<PedidoDeVenda>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarPedidosDeVendaFiltros? filtro = null,
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
                    // Mais recentes primeiro; número como desempate estável.
                    .OrderByDescending(p => p.DataCriacaoUtc)
                    .ThenByDescending(p => p.Numero)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<PedidoDeVenda>>.Sucesso(
                    new ResultadoPaginado<PedidoDeVenda>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<PedidoDeVenda>>.Falha(ex.Message);
            }
        }

        private IQueryable<PedidoDeVenda> CriarConsultaFiltrada(ListarPedidosDeVendaFiltros? filtro)
        {
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@id_cliente::bigint IS NULL OR id_cliente = @id_cliente)
                  AND (@status::int IS NULL OR status = @status)
                  AND (@data_inicio::timestamptz IS NULL OR data_criacao_utc >= @data_inicio)
                  AND (@data_fim::timestamptz IS NULL OR data_criacao_utc <= @data_fim)
                """;

            return _contexto.Set<PedidoDeVenda>().FromSqlRaw(
                sql,
                CriarParametro("id_cliente", NpgsqlDbType.Bigint, filtro?.IdCliente),
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
