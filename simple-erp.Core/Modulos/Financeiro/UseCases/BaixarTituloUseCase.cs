using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface IBaixarTituloUseCase
        : IUseCase<BaixarTituloEntrada, BaixarTituloSaida>
    {
    }

    public record BaixarTituloEntrada(long Id, decimal Valor) : IRequisicao<BaixarTituloSaida>;

    public record BaixarTituloSaida(
        long Id,
        string Status,
        decimal ValorBaixado,
        decimal SaldoDevedor);

    public sealed class BaixarTituloUseCase : IBaixarTituloUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public BaixarTituloUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<BaixarTituloSaida>> ExecutarAsync(BaixarTituloEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(BaixarTituloUseCase),
                ["TituloId"] = dados.Id,
                ["Valor"] = dados.Valor
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando baixa de título."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);
            var resultadoValor = Dinheiro.TentarCriar(dados.Valor);

            var validacao = Resultado.Combinar(resultadoId, resultadoValor);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<BaixarTituloSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoTitulo = await _unitOfWork.TitulosRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoTitulo.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<BaixarTituloSaida>.Falha(resultadoTitulo.Erros!);
            }

            var titulo = resultadoTitulo.Instancia;

            if (titulo is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de baixar título não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["TituloId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<BaixarTituloSaida>.Falha("TITULO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Baixa do título

                var resultadoBaixa = titulo.Baixar(resultadoValor.Instancia);

                if (resultadoBaixa.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao baixar o agregado Titulo.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["TituloId"] = titulo.Id.Valor,
                            ["Status"] = titulo.Status.ToString(),
                            ["Erros"] = resultadoBaixa.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<BaixarTituloSaida>.Falha(resultadoBaixa.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.TitulosRepository
                .AtualizarAsync(titulo, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<BaixarTituloSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir baixa de título.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<BaixarTituloSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Título baixado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TituloId"] = titulo.Id.Valor,
                    ["Status"] = titulo.Status.ToString(),
                    ["SaldoDevedor"] = titulo.SaldoDevedor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<BaixarTituloSaida>.Sucesso(
                new BaixarTituloSaida(
                    Id: titulo.Id.Valor,
                    Status: titulo.Status.ToString(),
                    ValorBaixado: titulo.ValorBaixado,
                    SaldoDevedor: titulo.SaldoDevedor));

            #endregion
        }
    }
}
