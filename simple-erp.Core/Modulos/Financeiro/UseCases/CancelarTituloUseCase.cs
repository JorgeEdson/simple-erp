using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface ICancelarTituloUseCase
        : IUseCase<CancelarTituloEntrada, CancelarTituloSaida>
    {
    }

    public record CancelarTituloEntrada(Guid Id) : IRequisicao<CancelarTituloSaida>;

    public record CancelarTituloSaida(
        Guid Id,
        string Status);

    public sealed class CancelarTituloUseCase : ICancelarTituloUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CancelarTituloUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CancelarTituloSaida>> ExecutarAsync(CancelarTituloEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CancelarTituloUseCase),
                ["TituloId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cancelamento de título."));

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

                return Resultado<CancelarTituloSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoTitulo = await _unitOfWork.TitulosRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

            if (resultadoTitulo.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarTituloSaida>.Falha(resultadoTitulo.Erros!);
            }

            var titulo = resultadoTitulo.Instancia;

            if (titulo is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de cancelar título não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["TituloId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CancelarTituloSaida>.Falha("TITULO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Cancelamento do título

                var resultadoCancelamento = titulo.Cancelar();

                if (resultadoCancelamento.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao cancelar o agregado Titulo.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["TituloId"] = titulo.Id,
                            ["Status"] = titulo.Status.ToString(),
                            ["Erros"] = resultadoCancelamento.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<CancelarTituloSaida>.Falha(resultadoCancelamento.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.TitulosRepository
                .AtualizarAsync(titulo, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarTituloSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarTituloSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Título cancelado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TituloId"] = titulo.Id,
                    ["Status"] = titulo.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CancelarTituloSaida>.Sucesso(
                new CancelarTituloSaida(
                    Id: titulo.Id,
                    Status: titulo.Status.ToString()));

            #endregion
        }
    }
}
