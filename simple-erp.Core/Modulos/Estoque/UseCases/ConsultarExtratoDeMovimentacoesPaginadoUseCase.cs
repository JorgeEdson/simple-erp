using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Estoque.UseCases
{
    public interface IConsultarExtratoDeMovimentacoesPaginadoUseCase
        : IUseCase<ConsultarExtratoDeMovimentacoesPaginadoEntrada, ConsultarExtratoDeMovimentacoesPaginadoSaida>
    {
    }

    public sealed record ConsultarExtratoDeMovimentacoesPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        long? IdProduto = null,
        TipoDeMovimentacao? Tipo = null,
        TipoOrigemMovimentacao? OrigemTipo = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null);

    public sealed record ConsultarExtratoDeMovimentacoesPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ExtratoDeMovimentacaoItemSaida> Itens);

    public sealed record ExtratoDeMovimentacaoItemSaida(
        long IdMovimentacao,
        long IdProduto,
        string Tipo,
        string Sentido,
        decimal Quantidade,
        decimal SaldoResultante,
        string OrigemTipo,
        long? OrigemIdReferencia,
        DateTime DataMovimentacaoUtc);

    public sealed record ListarMovimentacoesDeEstoqueFiltros(
        long? IdProduto = null,
        TipoDeMovimentacao? Tipo = null,
        TipoOrigemMovimentacao? OrigemTipo = null,
        DateTime? DataInicio = null,
        DateTime? DataFim = null);

    public sealed class ConsultarExtratoDeMovimentacoesPaginadoUseCase
        : IConsultarExtratoDeMovimentacoesPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ConsultarExtratoDeMovimentacoesPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ConsultarExtratoDeMovimentacoesPaginadoSaida>> ExecutarAsync(ConsultarExtratoDeMovimentacoesPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConsultarExtratoDeMovimentacoesPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["IdProduto"] = dados.IdProduto,
                ["Tipo"] = dados.Tipo?.ToString(),
                ["OrigemTipo"] = dados.OrigemTipo?.ToString(),
                ["DataInicio"] = dados.DataInicio,
                ["DataFim"] = dados.DataFim
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta do extrato de movimentações de estoque."));

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
                    Mensagem: "Falha na validação dos parâmetros do extrato de movimentações de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ConsultarExtratoDeMovimentacoesPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarMovimentacoesDeEstoqueFiltros(
                IdProduto: dados.IdProduto,
                Tipo: dados.Tipo,
                OrigemTipo: dados.OrigemTipo,
                DataInicio: dados.DataInicio,
                DataFim: dados.DataFim);

            var stopwatchListar = Stopwatch.StartNew();

            var resultadoPaginado = await _unitOfWork.MovimentacoesDeEstoqueRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtros,
                cancellationToken);

            stopwatchListar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta paginada do extrato de movimentações concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ListarPaginadoAsync",
                    ["DuracaoMs"] = stopwatchListar.ElapsedMilliseconds
                }));

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao consultar o extrato de movimentações de estoque no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ConsultarExtratoDeMovimentacoesPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var itens = pagina.Itens
                .Select(movimentacao => new ExtratoDeMovimentacaoItemSaida(
                    IdMovimentacao: movimentacao.Id.Valor,
                    IdProduto: movimentacao.IdProduto.Valor,
                    Tipo: movimentacao.Tipo.ToString(),
                    Sentido: movimentacao.Sentido.ToString(),
                    Quantidade: movimentacao.Quantidade,
                    SaldoResultante: movimentacao.SaldoResultante,
                    OrigemTipo: movimentacao.Origem.Tipo.ToString(),
                    OrigemIdReferencia: movimentacao.Origem.IdReferencia,
                    DataMovimentacaoUtc: movimentacao.DataMovimentacaoUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta do extrato de movimentações de estoque concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["NumeroPagina"] = pagina.NumeroPagina,
                    ["TamanhoPagina"] = pagina.TamanhoPagina,
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["TotalPaginas"] = pagina.TotalPaginas,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConsultarExtratoDeMovimentacoesPaginadoSaida>.Sucesso(
                new ConsultarExtratoDeMovimentacoesPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
