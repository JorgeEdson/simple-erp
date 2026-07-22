using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface ICancelarOrdemDeProducaoUseCase
        : IUseCase<CancelarOrdemDeProducaoEntrada, CancelarOrdemDeProducaoSaida>
    {
    }

    public record CancelarOrdemDeProducaoEntrada(long Id) : IRequisicao<CancelarOrdemDeProducaoSaida>;

    public record CancelarOrdemDeProducaoSaida(
        long Id,
        string Status);

    public sealed class CancelarOrdemDeProducaoUseCase : ICancelarOrdemDeProducaoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CancelarOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CancelarOrdemDeProducaoSaida>> ExecutarAsync(CancelarOrdemDeProducaoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CancelarOrdemDeProducaoUseCase),
                ["OrdemDeProducaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cancelamento de ordem de produção."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarOrdemDeProducaoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoOrdem.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarOrdemDeProducaoSaida>.Falha(resultadoOrdem.Erros!);
            }

            var ordem = resultadoOrdem.Instancia;

            if (ordem is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de cancelar ordem de produção não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CancelarOrdemDeProducaoSaida>.Falha("ORDEM_DE_PRODUCAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Execução das regras de negócio

                #region Cancelamento da ordem de produção

                var resultadoCancelar = ordem.Cancelar();

                if (resultadoCancelar.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao cancelar o agregado OrdemDeProducao.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["OrdemDeProducaoId"] = ordem.Id.Valor,
                            ["Status"] = ordem.Status.ToString(),
                            ["Erros"] = resultadoCancelar.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<CancelarOrdemDeProducaoSaida>.Falha(resultadoCancelar.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.OrdensDeProducaoRepository
                .AtualizarAsync(ordem, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarOrdemDeProducaoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir cancelamento de ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CancelarOrdemDeProducaoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção cancelada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id.Valor,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CancelarOrdemDeProducaoSaida>.Sucesso(
                new CancelarOrdemDeProducaoSaida(
                    Id: ordem.Id.Valor,
                    Status: ordem.Status.ToString()));

            #endregion
        }
    }
}
