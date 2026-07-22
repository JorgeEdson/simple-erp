using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Suprimentos.UseCases
{
    public interface IListarPedidosDeCompraPaginadoUseCase
        : IUseCase<ListarPedidosDeCompraPaginadoEntrada, ListarPedidosDeCompraPaginadoSaida>
    {
    }

    public sealed record ListarPedidosDeCompraPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        long? IdFornecedor = null,
        StatusPedidoDeCompra? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null) : IRequisicao<ListarPedidosDeCompraPaginadoSaida>;

    public sealed record ListarPedidosDeCompraPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ListarPedidosDeCompraPaginadoItemSaida> Itens);

    public sealed record ListarPedidosDeCompraPaginadoItemSaida(
        long Id,
        long IdFornecedor,
        string Status,
        decimal ValorTotal,
        int QuantidadeItens,
        DateTime DataCriacaoUtc);

    public sealed record ListarPedidosDeCompraFiltros(
        long? IdFornecedor = null,
        StatusPedidoDeCompra? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null);

    public sealed class ListarPedidosDeCompraPaginadoUseCase : IListarPedidosDeCompraPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarPedidosDeCompraPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarPedidosDeCompraPaginadoSaida>> ExecutarAsync(ListarPedidosDeCompraPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarPedidosDeCompraPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["IdFornecedor"] = dados.IdFornecedor,
                ["Status"] = dados.Status?.ToString(),
                ["DataInicio"] = dados.DataInicio,
                ["DataFim"] = dados.DataFim
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de pedidos de compra."));

            #endregion

            #region Validação dos parâmetros

            var erros = new List<string>();

            if (dados.NumeroPagina <= 0)
                erros.Add("NUMERO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina <= 0)
                erros.Add("TAMANHO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina > 100)
                erros.Add("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            if (dados.DataInicio.HasValue
                && dados.DataFim.HasValue
                && dados.DataInicio.Value > dados.DataFim.Value)
            {
                erros.Add("PERIODO_INVALIDO");
            }

            if (erros.Any())
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos parâmetros da listagem paginada de pedidos de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarPedidosDeCompraPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarPedidosDeCompraFiltros(
                IdFornecedor: dados.IdFornecedor,
                Status: dados.Status,
                DataInicio: dados.DataInicio,
                DataFim: dados.DataFim);

            var stopwatchListar = Stopwatch.StartNew();

            var resultadoPaginado = await _unitOfWork.PedidosDeCompraRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtros,
                cancellationToken);

            stopwatchListar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta paginada de pedidos de compra no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ListarPaginadoAsync",
                    ["DuracaoMs"] = stopwatchListar.ElapsedMilliseconds
                }));

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar pedidos de compra paginados no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarPedidosDeCompraPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(pedido => new ListarPedidosDeCompraPaginadoItemSaida(
                    Id: pedido.Id.Valor,
                    IdFornecedor: pedido.IdFornecedor.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    QuantidadeItens: pedido.Itens.Count,
                    DataCriacaoUtc: pedido.DataCriacaoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de pedidos de compra concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["NumeroPagina"] = pagina.NumeroPagina,
                    ["TamanhoPagina"] = pagina.TamanhoPagina,
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["TotalPaginas"] = pagina.TotalPaginas,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarPedidosDeCompraPaginadoSaida>.Sucesso(
                new ListarPedidosDeCompraPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
