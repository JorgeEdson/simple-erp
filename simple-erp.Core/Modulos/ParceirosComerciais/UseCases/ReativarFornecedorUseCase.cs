using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IReativarFornecedorUseCase
        : IUseCase<ReativarFornecedorEntrada, ReativarFornecedorSaida>
    {
    }

    public record ReativarFornecedorEntrada(long Id);

    public record ReativarFornecedorSaida(
        long Id,
        bool Ativo
    );

    public sealed class ReativarFornecedorUseCase : IReativarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ReativarFornecedorUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ReativarFornecedorSaida>> ExecutarAsync(
            ReativarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ReativarFornecedorUseCase),
                ["FornecedorId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando reativação de fornecedor."));

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para reativação de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha(resultadoId.Erros!);
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
                    Mensagem: "Falha ao obter fornecedor por id para reativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);
            }

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de reativação de fornecedor não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            var stopwatchAtivacao = Stopwatch.StartNew();

            var resultadoAtivacao = fornecedor.Ativar();

            stopwatchAtivacao.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Reativação do agregado Fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "Fornecedor.Ativar",
                    ["DuracaoMs"] = stopwatchAtivacao.ElapsedMilliseconds
                }));

            if (resultadoAtivacao.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao reativar agregado Fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoAtivacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha(resultadoAtivacao.Erros!);
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
                    Mensagem: "Falha ao atualizar fornecedor no repositório durante reativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da reativação de fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir reativação de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarFornecedorSaida>.Falha(resultadoSave.Erros!);
            }

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Fornecedor reativado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["FornecedorId"] = fornecedor.Id.Valor,
                    ["Ativo"] = fornecedor.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ReativarFornecedorSaida>.Sucesso(
                new ReativarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Ativo: fornecedor.Ativo));
        }
    }
}
