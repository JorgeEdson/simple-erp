using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IInativarFornecedorUseCase
       : IUseCase<InativarFornecedorEntrada, InativarFornecedorSaida>
    {
    }

    public record InativarFornecedorEntrada(long Id);

    public record InativarFornecedorSaida(
        long Id,
        bool Ativo
    );

    public sealed class InativarFornecedorUseCase : IInativarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public InativarFornecedorUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<InativarFornecedorSaida>> ExecutarAsync(
            InativarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(InativarFornecedorUseCase),
                ["FornecedorId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando inativação de fornecedor."));

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para inativação de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha(resultadoId.Erros!);
            }

            var stopwatchObterFornecedor = Stopwatch.StartNew();

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            stopwatchObterFornecedor.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de fornecedor por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObterFornecedor.ElapsedMilliseconds
                }));

            if (resultadoFornecedor.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter fornecedor por id para inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);
            }

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de inativação de fornecedor não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            var stopwatchInativacao = Stopwatch.StartNew();

            var resultadoInativacao = fornecedor.Inativar();

            stopwatchInativacao.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Inativação do agregado Fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "Fornecedor.Inativar",
                    ["DuracaoMs"] = stopwatchInativacao.ElapsedMilliseconds
                }));

            if (resultadoInativacao.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao inativar agregado Fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoInativacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha(resultadoInativacao.Erros!);
            }

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.FornecedoresRepository.AtualizarAsync(
                fornecedor,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de fornecedor no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar fornecedor no repositório durante inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da inativação de fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir inativação de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarFornecedorSaida>.Falha(resultadoSave.Erros!);
            }

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Fornecedor inativado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["FornecedorId"] = fornecedor.Id.Valor,
                    ["Ativo"] = fornecedor.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<InativarFornecedorSaida>.Sucesso(
                new InativarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Ativo: fornecedor.Ativo));
        }
    }
}
