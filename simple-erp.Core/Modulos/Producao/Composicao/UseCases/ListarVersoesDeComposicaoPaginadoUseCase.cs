using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IListarVersoesDeComposicaoPaginadoUseCase
        : IUseCase<ListarVersoesDeComposicaoPaginadoEntrada, ListarVersoesDeComposicaoPaginadoSaida>
    {
    }

    public sealed record ListarVersoesDeComposicaoPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        Guid IdProdutoFabricado,
        bool? ApenasAtivas = null) : IRequisicao<ListarVersoesDeComposicaoPaginadoSaida>;

    public sealed record ListarVersoesDeComposicaoPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<VersaoDeComposicaoItemSaida> Itens);

    public sealed record VersaoDeComposicaoItemSaida(
        Guid Id,
        Guid IdProdutoFabricado,
        int Versao,
        bool Ativa,
        int QuantidadeItens,
        DateTime DataCriacaoUtc);

    public sealed record ListarVersoesDeComposicaoFiltros(
        Guid IdProdutoFabricado,
        bool? ApenasAtivas = null);

    public sealed class ListarVersoesDeComposicaoPaginadoUseCase : IListarVersoesDeComposicaoPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarVersoesDeComposicaoPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarVersoesDeComposicaoPaginadoSaida>> ExecutarAsync(ListarVersoesDeComposicaoPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarVersoesDeComposicaoPaginadoUseCase),
                ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["ApenasAtivas"] = dados.ApenasAtivas
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem de versões de composição."));

            #endregion

            #region Validação do identificador

            if (dados.IdProdutoFabricado == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarVersoesDeComposicaoPaginadoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Validação dos parâmetros

            var erros = new List<string>();
            #endregion

            #region Consulta paginada

            var filtros = new ListarVersoesDeComposicaoFiltros(
                IdProdutoFabricado: dados.IdProdutoFabricado,
                ApenasAtivas: dados.ApenasAtivas);

            var resultadoPaginado = await _unitOfWork.ComposicoesDeProdutoRepository
                .ListarPorProdutoPaginadoAsync(
                    dados.NumeroPagina,
                    dados.TamanhoPagina,
                    filtros,
                    cancellationToken);

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar versões de composição no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarVersoesDeComposicaoPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(composicao => new VersaoDeComposicaoItemSaida(
                    Id: composicao.Id,
                    IdProdutoFabricado: composicao.IdProdutoFabricado,
                    Versao: composicao.Versao,
                    Ativa: composicao.Ativa,
                    QuantidadeItens: composicao.Itens.Count,
                    DataCriacaoUtc: composicao.DataCriacaoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem de versões de composição concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarVersoesDeComposicaoPaginadoSaida>.Sucesso(
                new ListarVersoesDeComposicaoPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
