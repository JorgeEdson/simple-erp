using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface IListarTitulosPaginadoUseCase
        : IUseCase<ListarTitulosPaginadoEntrada, ListarTitulosPaginadoSaida>
    {
    }

    public sealed record ListarTitulosPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        TipoDeTitulo? Tipo = null,
        StatusTitulo? Status = null,
        long? IdParceiro = null,
        DateTime? VencimentoInicio = null,
        DateTime? VencimentoFim = null) : IRequisicao<ListarTitulosPaginadoSaida>;

    public sealed record ListarTitulosPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ListarTitulosItemSaida> Itens);

    public sealed record ListarTitulosItemSaida(
        long Id,
        string Tipo,
        long IdParceiro,
        decimal ValorOriginal,
        decimal SaldoDevedor,
        string Status,
        DateTime DataVencimentoUtc);

    public sealed record ListarTitulosFiltros(
        TipoDeTitulo? Tipo = null,
        StatusTitulo? Status = null,
        long? IdParceiro = null,
        DateTime? VencimentoInicio = null,
        DateTime? VencimentoFim = null);

    public sealed class ListarTitulosPaginadoUseCase : IListarTitulosPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarTitulosPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarTitulosPaginadoSaida>> ExecutarAsync(ListarTitulosPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarTitulosPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["Tipo"] = dados.Tipo?.ToString(),
                ["Status"] = dados.Status?.ToString()
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de títulos."));

            #endregion

            #region Validação dos parâmetros

            var erros = new List<string>();

            if (dados.NumeroPagina <= 0)
                erros.Add("NUMERO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina <= 0)
                erros.Add("TAMANHO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina > 100)
                erros.Add("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            if (dados.VencimentoInicio.HasValue
                && dados.VencimentoFim.HasValue
                && dados.VencimentoInicio.Value > dados.VencimentoFim.Value)
            {
                erros.Add("PERIODO_INVALIDO");
            }

            if (erros.Any())
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos parâmetros da listagem de títulos.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarTitulosPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarTitulosFiltros(
                Tipo: dados.Tipo,
                Status: dados.Status,
                IdParceiro: dados.IdParceiro,
                VencimentoInicio: dados.VencimentoInicio,
                VencimentoFim: dados.VencimentoFim);

            var resultadoPaginado = await _unitOfWork.TitulosRepository
                .ListarPaginadoAsync(
                    dados.NumeroPagina,
                    dados.TamanhoPagina,
                    filtros,
                    cancellationToken);

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar títulos no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ListarTitulosPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(titulo => new ListarTitulosItemSaida(
                    Id: titulo.Id.Valor,
                    Tipo: titulo.Tipo.ToString(),
                    IdParceiro: titulo.IdParceiro.Valor,
                    ValorOriginal: titulo.ValorOriginal,
                    SaldoDevedor: titulo.SaldoDevedor,
                    Status: titulo.Status.ToString(),
                    DataVencimentoUtc: titulo.DataVencimentoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de títulos concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarTitulosPaginadoSaida>.Sucesso(
                new ListarTitulosPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
