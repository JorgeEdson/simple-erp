using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.Suprimentos
{
    /// <summary>
    /// Repositório do agregado PedidoDeCompra. Segue os contratos dos demais módulos:
    /// - nenhum SaveChanges (a transação pertence ao UnitOfWork);
    /// - ObterPorIdAsync retorna a entidade RASTREADA (caminho de edição/aprovação/efetivação);
    /// - "não encontrado" é Sucesso com instância nula, não falha de infraestrutura;
    /// - falhas de infra viram Resultado.Falha.
    ///
    /// Os itens viajam junto no jsonb do pedido — carregar o pedido já traz seus itens.
    /// </summary>
    public sealed class PedidoDeCompraRepository : IPedidoDeCompraRepository
    {
        private const string TabelaComSchema = "suprimentos.pedidos_de_compra";

        private readonly SimpleErpDbContext _contexto;

        public PedidoDeCompraRepository(SimpleErpDbContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<Resultado<bool>> AdicionarAsync(
            PedidoDeCompra pedidoDeCompra, CancellationToken cancellationToken = default)
        {
            try
            {
                await _contexto.Set<PedidoDeCompra>().AddAsync(pedidoDeCompra, cancellationToken);
                return Resultado<bool>.Sucesso(true);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public Task<Resultado<bool>> AtualizarAsync(
            PedidoDeCompra pedidoDeCompra, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_contexto.Entry(pedidoDeCompra).State == EntityState.Detached)
                    _contexto.Set<PedidoDeCompra>().Update(pedidoDeCompra);

                return Task.FromResult(Resultado<bool>.Sucesso(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Resultado<bool>.Falha(ex.Message));
            }
        }

        public async Task<Resultado<PedidoDeCompra>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Rastreado: caminho de escrita (editar itens / aprovar / efetivar → atualizar).
                var pedido = await _contexto.Set<PedidoDeCompra>()
                    .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

                // Contrato: Sucesso com instância nula quando não encontrado —
                // o use case decide o erro de negócio (ex.: PEDIDO_DE_COMPRA_NAO_ENCONTRADO).
                return Resultado<PedidoDeCompra>.Sucesso(pedido!);
            }
            catch (Exception ex)
            {
                return Resultado<PedidoDeCompra>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default)
        {
            try
            {
                var existe = await _contexto.Set<PedidoDeCompra>()
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == id, cancellationToken);

                return Resultado<bool>.Sucesso(existe);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }

        public async Task<Resultado<ResultadoPaginado<PedidoDeCompra>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarPedidosDeCompraFiltros? filtro = null,
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
                    // Mais recentes primeiro; Id como desempate estável.
                    .OrderByDescending(p => p.DataCriacaoUtc)
                    .ThenByDescending(p => p.Id)
                    .Skip((pagina - 1) * tamanho)
                    .Take(tamanho)
                    .ToListAsync(cancellationToken);

                return Resultado<ResultadoPaginado<PedidoDeCompra>>.Sucesso(
                    new ResultadoPaginado<PedidoDeCompra>(
                        Itens: itens,
                        NumeroPagina: pagina,
                        TamanhoPagina: tamanho,
                        TotalRegistros: total));
            }
            catch (Exception ex)
            {
                return Resultado<ResultadoPaginado<PedidoDeCompra>>.Falha(ex.Message);
            }
        }

        private IQueryable<PedidoDeCompra> CriarConsultaFiltrada(ListarPedidosDeCompraFiltros? filtro)
        {
            // Filtros opcionais (parâmetro nulo desativa a condição). O intervalo de
            // datas incide sobre a data do pedido (data de criação). Tipos explícitos
            // garantem os casts com DBNull.
            const string sql = $"""
                SELECT * FROM {TabelaComSchema}
                WHERE (@id_fornecedor::bigint IS NULL OR id_fornecedor = @id_fornecedor)
                  AND (@status::int IS NULL OR status = @status)
                  AND (@data_inicio::timestamptz IS NULL OR data_criacao_utc >= @data_inicio)
                  AND (@data_fim::timestamptz IS NULL OR data_criacao_utc <= @data_fim)
                """;

            return _contexto.Set<PedidoDeCompra>().FromSqlRaw(
                sql,
                CriarParametro("id_fornecedor", NpgsqlDbType.Bigint, filtro?.IdFornecedor),
                CriarParametro("status", NpgsqlDbType.Integer, (int?)filtro?.Status),
                CriarParametro("data_inicio", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataInicio)),
                CriarParametro("data_fim", NpgsqlDbType.TimestampTz, NormalizarUtc(filtro?.DataFim)));
        }

        private static NpgsqlParameter CriarParametro(string nome, NpgsqlDbType tipo, object? valor) =>
            new(nome, tipo) { Value = valor ?? DBNull.Value };

        private static DateTime? NormalizarUtc(DateTime? valor) =>
            valor is null
                ? null
                : DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc);
    }
}
