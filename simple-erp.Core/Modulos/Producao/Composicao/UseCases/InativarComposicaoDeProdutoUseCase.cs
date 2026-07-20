using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IInativarComposicaoDeProdutoUseCase
        : IUseCase<InativarComposicaoDeProdutoEntrada, InativarComposicaoDeProdutoSaida>
    {
    }

    public record InativarComposicaoDeProdutoEntrada(long Id);

    public record InativarComposicaoDeProdutoSaida(
        long Id,
        long IdProdutoFabricado,
        int Versao,
        bool Ativa);

    public sealed class InativarComposicaoDeProdutoUseCase : IInativarComposicaoDeProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public InativarComposicaoDeProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<InativarComposicaoDeProdutoSaida>> ExecutarAsync(InativarComposicaoDeProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(InativarComposicaoDeProdutoUseCase),
                ["ComposicaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando inativação de composição de produto."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<InativarComposicaoDeProdutoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoComposicao = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoComposicao.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<InativarComposicaoDeProdutoSaida>.Falha(resultadoComposicao.Erros!);
            }

            var composicao = resultadoComposicao.Instancia;

            if (composicao is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de inativar composição não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ComposicaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<InativarComposicaoDeProdutoSaida>.Falha("COMPOSICAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Execução das regras de negócio

                #region Inativação da composição

                composicao.Inativar();

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.ComposicoesDeProdutoRepository
                .AtualizarAsync(composicao, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<InativarComposicaoDeProdutoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir inativação de composição de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<InativarComposicaoDeProdutoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Composição de produto inativada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ComposicaoId"] = composicao.Id.Valor,
                    ["Versao"] = composicao.Versao,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<InativarComposicaoDeProdutoSaida>.Sucesso(
                new InativarComposicaoDeProdutoSaida(
                    Id: composicao.Id.Valor,
                    IdProdutoFabricado: composicao.IdProdutoFabricado.Valor,
                    Versao: composicao.Versao,
                    Ativa: composicao.Ativa));

            #endregion
        }
    }
}
