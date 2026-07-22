using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IInativarClienteUseCase : IUseCase<InativarClienteEntrada, InativarClienteSaida>
    {
    }

    public record InativarClienteEntrada(long Id) : IRequisicao<InativarClienteSaida>;

    public record InativarClienteSaida(
       long Id,
       bool Ativo
    );

    public sealed class InativarClienteUseCase : IInativarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public InativarClienteUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<InativarClienteSaida>> ExecutarAsync(InativarClienteEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(InativarClienteUseCase),
                ["ClienteId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando inativação de cliente."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para inativação de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarClienteSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterCliente = Stopwatch.StartNew();

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoCliente.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter cliente por id para inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoCliente.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarClienteSaida>.Falha(resultadoCliente.Erros!);
            }

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de inativação de cliente não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarClienteSaida>.Falha("CLIENTE_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Inativação do cliente

                var stopwatchInativacao = Stopwatch.StartNew();

                var resultadoInativacao = cliente.Inativar();

                stopwatchInativacao.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Inativação do agregado Cliente concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoDominio"] = "Cliente.Inativar",
                        ["DuracaoMs"] = stopwatchInativacao.ElapsedMilliseconds
                    }));

                if (resultadoInativacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao inativar agregado Cliente.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ClienteId"] = cliente.Id.Valor,
                            ["Erros"] = resultadoInativacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<InativarClienteSaida>.Falha(resultadoInativacao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.ClientesRepository.AtualizarAsync(
                cliente,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de cliente no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar cliente no repositório durante inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = cliente.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarClienteSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da inativação de cliente concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir inativação de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = cliente.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarClienteSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Cliente inativado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ClienteId"] = cliente.Id.Valor,
                    ["Ativo"] = cliente.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<InativarClienteSaida>.Sucesso(
                new InativarClienteSaida(
                    Id: cliente.Id.Valor,
                    Ativo: cliente.Ativo));

            #endregion
        }
    }
}
