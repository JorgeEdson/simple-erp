using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IAtivarComposicaoDeProdutoUseCase
        : IUseCase<AtivarComposicaoDeProdutoEntrada, AtivarComposicaoDeProdutoSaida>
    {
    }

    public record AtivarComposicaoDeProdutoEntrada(long Id);

    public record AtivarComposicaoDeProdutoSaida(
        long Id,
        long IdProdutoFabricado,
        int Versao,
        bool Ativa);

    public sealed class AtivarComposicaoDeProdutoUseCase : IAtivarComposicaoDeProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AtivarComposicaoDeProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AtivarComposicaoDeProdutoSaida>> ExecutarAsync(AtivarComposicaoDeProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AtivarComposicaoDeProdutoUseCase),
                ["ComposicaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando ativação de composição de produto."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AtivarComposicaoDeProdutoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoComposicao = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoComposicao.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AtivarComposicaoDeProdutoSaida>.Falha(resultadoComposicao.Erros!);
            }

            var composicao = resultadoComposicao.Instancia;

            if (composicao is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de ativar composição não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ComposicaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AtivarComposicaoDeProdutoSaida>.Falha("COMPOSICAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Validação de pré-condições

            if (composicao.Ativa)
            {
                stopwatchUseCase.Stop();
                return Resultado<AtivarComposicaoDeProdutoSaida>.Sucesso(Mapear(composicao));
            }

            #endregion

            #region Execução das regras de negócio

                #region Ativação da versão

                composicao.Ativar();

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.ComposicoesDeProdutoRepository
                .AtualizarAsync(composicao, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AtivarComposicaoDeProdutoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir ativação de composição de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AtivarComposicaoDeProdutoSaida>.Falha(resultadoSave.Erros!);
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
                Mensagem: "Composição de produto ativada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ComposicaoId"] = composicao.Id.Valor,
                    ["Versao"] = composicao.Versao,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AtivarComposicaoDeProdutoSaida>.Sucesso(Mapear(composicao));

            #endregion
        }

        private static AtivarComposicaoDeProdutoSaida Mapear(ComposicaoDeProduto composicao)
        {
            return new AtivarComposicaoDeProdutoSaida(
                Id: composicao.Id.Valor,
                IdProdutoFabricado: composicao.IdProdutoFabricado.Valor,
                Versao: composicao.Versao,
                Ativa: composicao.Ativa);
        }
    }
}
