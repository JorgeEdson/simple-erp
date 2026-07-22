using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IListarPedidosDeVendaPaginadoUseCase
        : IUseCase<ListarPedidosDeVendaPaginadoEntrada, ListarPedidosDeVendaPaginadoSaida>
    {
    }

    public sealed record ListarPedidosDeVendaPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        long? IdCliente = null,
        StatusPedidoDeVenda? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null) : IRequisicao<ListarPedidosDeVendaPaginadoSaida>;

    public sealed record ListarPedidosDeVendaPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ListarPedidosDeVendaItemSaida> Itens);

    public sealed record ListarPedidosDeVendaItemSaida(
        long Id,
        int Numero,
        long IdCliente,
        string Status,
        decimal ValorTotal,
        int QuantidadeItens,
        DateTime DataCriacaoUtc);

    public sealed record ListarPedidosDeVendaFiltros(
        long? IdCliente = null,
        StatusPedidoDeVenda? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null);

    public sealed class ListarPedidosDeVendaPaginadoUseCase : IListarPedidosDeVendaPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarPedidosDeVendaPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarPedidosDeVendaPaginadoSaida>> ExecutarAsync(ListarPedidosDeVendaPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarPedidosDeVendaPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["IdCliente"] = dados.IdCliente,
                ["Status"] = dados.Status?.ToString()
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de pedidos de venda."));

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
                    Mensagem: "Falha na validação dos parâmetros da listagem de pedidos de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarPedidosDeVendaPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarPedidosDeVendaFiltros(
                IdCliente: dados.IdCliente,
                Status: dados.Status,
                DataInicio: dados.DataInicio,
                DataFim: dados.DataFim);

            var resultadoPaginado = await _unitOfWork.PedidosDeVendaRepository
                .ListarPaginadoAsync(
                    dados.NumeroPagina,
                    dados.TamanhoPagina,
                    filtros,
                    cancellationToken);

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar pedidos de venda no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarPedidosDeVendaPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(pedido => new ListarPedidosDeVendaItemSaida(
                    Id: pedido.Id.Valor,
                    Numero: pedido.Numero,
                    IdCliente: pedido.IdCliente.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    QuantidadeItens: pedido.Itens.Count,
                    DataCriacaoUtc: pedido.DataCriacaoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de pedidos de venda concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarPedidosDeVendaPaginadoSaida>.Sucesso(
                new ListarPedidosDeVendaPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
