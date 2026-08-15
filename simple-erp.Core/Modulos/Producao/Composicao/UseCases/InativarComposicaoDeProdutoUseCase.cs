using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IInativarComposicaoDeProdutoUseCase
        : IUseCase<InativarComposicaoDeProdutoEntrada, InativarComposicaoDeProdutoSaida>
    {
    }

    public record InativarComposicaoDeProdutoEntrada(Guid Id) : IRequisicao<InativarComposicaoDeProdutoSaida>;

    public record InativarComposicaoDeProdutoSaida(
        Guid Id,
        Guid IdProdutoFabricado,
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

                return Resultado<InativarComposicaoDeProdutoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoComposicao = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

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
                    ["ComposicaoId"] = composicao.Id,
                    ["Versao"] = composicao.Versao,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<InativarComposicaoDeProdutoSaida>.Sucesso(
                new InativarComposicaoDeProdutoSaida(
                    Id: composicao.Id,
                    IdProdutoFabricado: composicao.IdProdutoFabricado,
                    Versao: composicao.Versao,
                    Ativa: composicao.Ativa));

            #endregion
        }
    }
}
