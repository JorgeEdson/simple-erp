using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Estoque.UseCases
{
    public interface IConsultarSaldoDeEstoqueUseCase
        : IUseCase<ConsultarSaldoDeEstoqueEntrada, ConsultarSaldoDeEstoqueSaida>
    {
    }

    public record ConsultarSaldoDeEstoqueEntrada(Guid IdProduto) : IRequisicao<ConsultarSaldoDeEstoqueSaida>;

    public record ConsultarSaldoDeEstoqueSaida(
        Guid IdProduto,
        decimal QuantidadeAtual,
        bool PossuiRegistroDeSaldo);

    public sealed class ConsultarSaldoDeEstoqueUseCase : IConsultarSaldoDeEstoqueUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ConsultarSaldoDeEstoqueUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ConsultarSaldoDeEstoqueSaida>> ExecutarAsync(ConsultarSaldoDeEstoqueEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConsultarSaldoDeEstoqueUseCase),
                ["IdProduto"] = dados.IdProduto
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta de saldo de estoque."));

            #endregion

            #region Validação do identificador

            if (dados.IdProduto == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProduto"] = dados.IdProduto,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ConsultarSaldoDeEstoqueSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchExiste = Stopwatch.StartNew();

            var saldoExiste = await _unitOfWork.SaldosDeEstoqueRepository.ExistePorProdutoAsync(
                dados.IdProduto,
                cancellationToken);

            stopwatchExiste.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de saldo de estoque concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ExistePorProdutoAsync",
                    ["DuracaoMs"] = stopwatchExiste.ElapsedMilliseconds
                }));

            if (saldoExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência de saldo de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = saldoExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ConsultarSaldoDeEstoqueSaida>.Falha(saldoExiste.Erros!);
            }

            var quantidadeAtual = 0m;
            var possuiRegistro = saldoExiste.Instancia;

            if (possuiRegistro)
            {
                var resultadoSaldo = await _unitOfWork.SaldosDeEstoqueRepository.ObterPorProdutoAsync(
                    dados.IdProduto,
                    cancellationToken);

                if (resultadoSaldo.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao obter saldo de estoque por produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoSaldo.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<ConsultarSaldoDeEstoqueSaida>.Falha(resultadoSaldo.Erros!);
                }

                quantidadeAtual = resultadoSaldo.Instancia!.QuantidadeAtual;
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta de saldo de estoque concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["IdProduto"] = dados.IdProduto,
                    ["QuantidadeAtual"] = quantidadeAtual,
                    ["PossuiRegistroDeSaldo"] = possuiRegistro,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConsultarSaldoDeEstoqueSaida>.Sucesso(
                new ConsultarSaldoDeEstoqueSaida(
                    IdProduto: dados.IdProduto,
                    QuantidadeAtual: quantidadeAtual,
                    PossuiRegistroDeSaldo: possuiRegistro));

            #endregion
        }
    }
}
