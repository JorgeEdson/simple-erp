using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IListarOrdensDeProducaoPaginadoUseCase
        : IUseCase<ListarOrdensDeProducaoPaginadoEntrada, ListarOrdensDeProducaoPaginadoSaida>
    {
    }

    public sealed record ListarOrdensDeProducaoPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        long? IdProdutoFabricado = null,
        StatusOrdemDeProducao? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null) : IRequisicao<ListarOrdensDeProducaoPaginadoSaida>;

    public sealed record ListarOrdensDeProducaoPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ListarOrdensDeProducaoItemSaida> Itens);

    public sealed record ListarOrdensDeProducaoItemSaida(
        long Id,
        long IdProdutoFabricado,
        decimal QuantidadeAProduzir,
        string Status,
        int QuantidadeNecessidades,
        DateTime DataCriacaoUtc);

    public sealed record ListarOrdensDeProducaoFiltros(
        long? IdProdutoFabricado = null,
        StatusOrdemDeProducao? Status = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null);

    public sealed class ListarOrdensDeProducaoPaginadoUseCase : IListarOrdensDeProducaoPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarOrdensDeProducaoPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarOrdensDeProducaoPaginadoSaida>> ExecutarAsync(ListarOrdensDeProducaoPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarOrdensDeProducaoPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                ["Status"] = dados.Status?.ToString()
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de ordens de produção."));

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
                    Mensagem: "Falha na validação dos parâmetros da listagem de ordens de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarOrdensDeProducaoPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarOrdensDeProducaoFiltros(
                IdProdutoFabricado: dados.IdProdutoFabricado,
                Status: dados.Status,
                DataInicio: dados.DataInicio,
                DataFim: dados.DataFim);

            var resultadoPaginado = await _unitOfWork.OrdensDeProducaoRepository
                .ListarPaginadoAsync(
                    dados.NumeroPagina,
                    dados.TamanhoPagina,
                    filtros,
                    cancellationToken);

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar ordens de produção no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarOrdensDeProducaoPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(ordem => new ListarOrdensDeProducaoItemSaida(
                    Id: ordem.Id.Valor,
                    IdProdutoFabricado: ordem.IdProdutoFabricado.Valor,
                    QuantidadeAProduzir: ordem.QuantidadeAProduzir,
                    Status: ordem.Status.ToString(),
                    QuantidadeNecessidades: ordem.Necessidades.Count,
                    DataCriacaoUtc: ordem.DataCriacaoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de ordens de produção concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarOrdensDeProducaoPaginadoSaida>.Sucesso(
                new ListarOrdensDeProducaoPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
