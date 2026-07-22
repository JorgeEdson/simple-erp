using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface IObterTituloPorIdUseCase
        : IUseCase<ObterTituloPorIdEntrada, ObterTituloPorIdSaida>
    {
    }

    public record ObterTituloPorIdEntrada(long Id) : IRequisicao<ObterTituloPorIdSaida>;

    public record BaixaDoTituloSaida(
        decimal Montante,
        DateTime DataUtc);

    public record ObterTituloPorIdSaida(
        long Id,
        string Tipo,
        long IdParceiro,
        string OrigemTipo,
        long? OrigemIdReferencia,
        decimal ValorOriginal,
        decimal ValorBaixado,
        decimal SaldoDevedor,
        string Status,
        DateTime DataVencimentoUtc,
        IReadOnlyCollection<BaixaDoTituloSaida> Baixas);

    public sealed class ObterTituloPorIdUseCase : IObterTituloPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterTituloPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterTituloPorIdSaida>> ExecutarAsync(ObterTituloPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterTituloPorIdUseCase),
                ["TituloId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta de título por id."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterTituloPorIdSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoTitulo = await _unitOfWork.TitulosRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoTitulo.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterTituloPorIdSaida>.Falha(resultadoTitulo.Erros!);
            }

            var titulo = resultadoTitulo.Instancia;

            if (titulo is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Título não encontrado na consulta por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["TituloId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ObterTituloPorIdSaida>.Falha("TITULO_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var baixas = titulo.Baixas
                .Select(baixa => new BaixaDoTituloSaida(baixa.Montante, baixa.DataUtc))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta de título por id concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TituloId"] = titulo.Id.Valor,
                    ["Status"] = titulo.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterTituloPorIdSaida>.Sucesso(
                new ObterTituloPorIdSaida(
                    Id: titulo.Id.Valor,
                    Tipo: titulo.Tipo.ToString(),
                    IdParceiro: titulo.IdParceiro.Valor,
                    OrigemTipo: titulo.Origem.Tipo.ToString(),
                    OrigemIdReferencia: titulo.Origem.IdReferencia,
                    ValorOriginal: titulo.ValorOriginal,
                    ValorBaixado: titulo.ValorBaixado,
                    SaldoDevedor: titulo.SaldoDevedor,
                    Status: titulo.Status.ToString(),
                    DataVencimentoUtc: titulo.DataVencimentoUtc,
                    Baixas: baixas));

            #endregion
        }
    }
}
