using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.Producao.Entidades;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IConcluirOrdemDeProducaoUseCase
        : IUseCase<ConcluirOrdemDeProducaoEntrada, ConcluirOrdemDeProducaoSaida>
    {
    }

    public record ConcluirOrdemDeProducaoEntrada(Guid Id) : IRequisicao<ConcluirOrdemDeProducaoSaida>;

    public record ConcluirOrdemDeProducaoSaida(
        Guid Id,
        string Status,
        Guid IdProdutoFabricado,
        decimal QuantidadeProduzida,
        int QuantidadeInsumosConsumidos);

    public sealed class ConcluirOrdemDeProducaoUseCase : IConcluirOrdemDeProducaoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ConcluirOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ConcluirOrdemDeProducaoSaida>> ExecutarAsync(ConcluirOrdemDeProducaoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConcluirOrdemDeProducaoUseCase),
                ["OrdemDeProducaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando conclusão de ordem de produção."));

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

                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

            if (resultadoOrdem.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoOrdem.Erros!);
            }

            var ordem = resultadoOrdem.Instancia;

            if (ordem is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de concluir ordem de produção não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha("ORDEM_DE_PRODUCAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Validação de pré-condições

            if (ordem.EstaConcluida)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));
            }

            if (!ordem.EstaConfirmada)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(
                    "ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");
            }

            #endregion

            #region Execução das regras de negócio

                #region Conclusão da ordem de produção

                var resultadoConcluir = ordem.Concluir();

                if (resultadoConcluir.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoConcluir.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.OrdensDeProducaoRepository
                .AtualizarAsync(ordem, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir conclusão de ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion
            

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConcluirOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));

            #endregion
        }

        private static ConcluirOrdemDeProducaoSaida Mapear(OrdemDeProducao ordem)
        {
            return new ConcluirOrdemDeProducaoSaida(
                Id: ordem.Id,
                Status: ordem.Status.ToString(),
                IdProdutoFabricado: ordem.IdProdutoFabricado,
                QuantidadeProduzida: ordem.QuantidadeAProduzir,
                QuantidadeInsumosConsumidos: ordem.Necessidades.Count);
        }
    }
}
