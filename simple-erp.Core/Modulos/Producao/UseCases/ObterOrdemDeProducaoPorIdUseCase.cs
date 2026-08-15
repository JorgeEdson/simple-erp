using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IObterOrdemDeProducaoPorIdUseCase
        : IUseCase<ObterOrdemDeProducaoPorIdEntrada, ObterOrdemDeProducaoPorIdSaida>
    {
    }

    public record ObterOrdemDeProducaoPorIdEntrada(Guid Id) : IRequisicao<ObterOrdemDeProducaoPorIdSaida>;

    public record OrdemDeProducaoNecessidadeSaida(
        Guid IdInsumo,
        decimal QuantidadeNecessaria);

    public record ObterOrdemDeProducaoPorIdSaida(
        Guid Id,
        Guid IdProdutoFabricado,
        Guid IdComposicao,
        decimal QuantidadeAProduzir,
        string Status,
        IReadOnlyCollection<OrdemDeProducaoNecessidadeSaida> Necessidades);

    public sealed class ObterOrdemDeProducaoPorIdUseCase : IObterOrdemDeProducaoPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterOrdemDeProducaoPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterOrdemDeProducaoPorIdSaida>> ExecutarAsync(ObterOrdemDeProducaoPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterOrdemDeProducaoPorIdUseCase),
                ["OrdemDeProducaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta de ordem de produção por id."));

            #endregion

            #region Validação do identificador

            if (dados.Id == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Id"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterOrdemDeProducaoPorIdSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

            if (resultadoOrdem.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterOrdemDeProducaoPorIdSaida>.Falha(resultadoOrdem.Erros!);
            }

            var ordem = resultadoOrdem.Instancia;

            if (ordem is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Ordem de produção não encontrada na consulta por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ObterOrdemDeProducaoPorIdSaida>.Falha("ORDEM_DE_PRODUCAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Mapeamento da saída

            var necessidades = ordem.Necessidades
                .Select(necessidade => new OrdemDeProducaoNecessidadeSaida(
                    IdInsumo: necessidade.IdInsumo,
                    QuantidadeNecessaria: necessidade.QuantidadeNecessaria))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta de ordem de produção por id concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterOrdemDeProducaoPorIdSaida>.Sucesso(
                new ObterOrdemDeProducaoPorIdSaida(
                    Id: ordem.Id,
                    IdProdutoFabricado: ordem.IdProdutoFabricado,
                    IdComposicao: ordem.IdComposicao,
                    QuantidadeAProduzir: ordem.QuantidadeAProduzir,
                    Status: ordem.Status.ToString(),
                    Necessidades: necessidades));

            #endregion
        }
    }
}
