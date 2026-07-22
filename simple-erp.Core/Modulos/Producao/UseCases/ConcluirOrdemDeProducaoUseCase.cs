using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IConcluirOrdemDeProducaoUseCase
        : IUseCase<ConcluirOrdemDeProducaoEntrada, ConcluirOrdemDeProducaoSaida>
    {
    }

    public record ConcluirOrdemDeProducaoEntrada(long Id) : IRequisicao<ConcluirOrdemDeProducaoSaida>;

    public record ConcluirOrdemDeProducaoSaida(
        long Id,
        string Status,
        long IdProdutoFabricado,
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

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

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

            // Os eventos de domínio produzidos por este agregado NÃO são despachados
            // aqui. O interceptor de persistência os gravou na caixa de saída dentro da
            // mesma transação do SaveChanges acima, e o worker que consome o outbox os
            // entrega aos manipuladores fora desta requisição.
            //
            // Duas consequências que valem ser ditas em voz alta: a resposta ao usuário
            // não espera pelos efeitos em outros contextos delimitados, e nenhum efeito
            // se perde caso a aplicação caia logo após a confirmação.

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id.Valor,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConcluirOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));

            #endregion
        }

        private static ConcluirOrdemDeProducaoSaida Mapear(OrdemDeProducao ordem)
        {
            return new ConcluirOrdemDeProducaoSaida(
                Id: ordem.Id.Valor,
                Status: ordem.Status.ToString(),
                IdProdutoFabricado: ordem.IdProdutoFabricado.Valor,
                QuantidadeProduzida: ordem.QuantidadeAProduzir,
                QuantidadeInsumosConsumidos: ordem.Necessidades.Count);
        }
    }
}
