using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IConfirmarOrdemDeProducaoUseCase
        : IUseCase<ConfirmarOrdemDeProducaoEntrada, ConfirmarOrdemDeProducaoSaida>
    {
    }

    public record ConfirmarOrdemDeProducaoEntrada(long Id) : IRequisicao<ConfirmarOrdemDeProducaoSaida>;

    public record ConfirmarOrdemDeProducaoSaida(
        long Id,
        string Status);

    public sealed class ConfirmarOrdemDeProducaoUseCase : IConfirmarOrdemDeProducaoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ConfirmarOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ConfirmarOrdemDeProducaoSaida>> ExecutarAsync(ConfirmarOrdemDeProducaoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConfirmarOrdemDeProducaoUseCase),
                ["OrdemDeProducaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando confirmação de ordem de produção."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoOrdem.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoOrdem.Erros!);
            }

            var ordem = resultadoOrdem.Instancia;

            if (ordem is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de confirmar ordem de produção não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha("ORDEM_DE_PRODUCAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Validação de pré-condições

            var resultadoDisponibilidade = await ValidarDisponibilidadeDeEstoqueAsync(ordem, cancellationToken);

            if (resultadoDisponibilidade.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Confirmação bloqueada por indisponibilidade de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = ordem.Id.Valor,
                        ["Erros"] = resultadoDisponibilidade.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoDisponibilidade.Erros!);
            }

            #endregion

            #region Execução das regras de negócio

                #region Confirmação da ordem de produção

                var resultadoConfirmar = ordem.Confirmar();

                if (resultadoConfirmar.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoConfirmar.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.OrdensDeProducaoRepository
                .AtualizarAsync(ordem, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir confirmação de ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConfirmarOrdemDeProducaoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção confirmada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id.Valor,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConfirmarOrdemDeProducaoSaida>.Sucesso(
                new ConfirmarOrdemDeProducaoSaida(
                    Id: ordem.Id.Valor,
                    Status: ordem.Status.ToString()));

            #endregion
        }

        private async Task<Resultado<bool>> ValidarDisponibilidadeDeEstoqueAsync(
            OrdemDeProducao ordem,
            CancellationToken cancellationToken)
        {
            var insuficientes = new List<string>();

            foreach (var necessidade in ordem.Necessidades)
            {
                var resultadoIdInsumo = Id.TentarCriar(necessidade.IdInsumo);

                if (resultadoIdInsumo.EhFalha)
                    return Resultado<bool>.Falha(resultadoIdInsumo.Erros!);

                var disponivel = 0m;

                var existeSaldo = await _unitOfWork.SaldosDeEstoqueRepository
                    .ExistePorProdutoAsync(resultadoIdInsumo.Instancia, cancellationToken);

                if (existeSaldo.EhFalha)
                    return Resultado<bool>.Falha(existeSaldo.Erros!);

                if (existeSaldo.Instancia)
                {
                    var resultadoSaldo = await _unitOfWork.SaldosDeEstoqueRepository
                        .ObterPorProdutoAsync(resultadoIdInsumo.Instancia, cancellationToken);

                    if (resultadoSaldo.EhFalha)
                        return Resultado<bool>.Falha(resultadoSaldo.Erros!);

                    disponivel = resultadoSaldo.Instancia!.QuantidadeAtual;
                }

                if (disponivel < necessidade.QuantidadeNecessaria)
                {
                    insuficientes.Add(
                        $"INSUMO_INSUFICIENTE|IdInsumo={necessidade.IdInsumo}" +
                        $"|Necessario={necessidade.QuantidadeNecessaria}|Disponivel={disponivel}");
                }
            }

            if (insuficientes.Count > 0)
            {
                var erros = new List<string> { "ESTOQUE_INSUFICIENTE" };
                erros.AddRange(insuficientes);
                return Resultado<bool>.Falha(erros);
            }

            return Resultado<bool>.Sucesso(true);
        }
    }
}
