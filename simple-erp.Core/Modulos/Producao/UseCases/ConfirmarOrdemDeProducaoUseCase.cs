using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Servicos;
using simple_erp.Core.Modulos.Producao.Entidades;
using System.Diagnostics;
using System.Linq;

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
        private readonly IServicoDeDisponibilidadeDeEstoque _servicoDeDisponibilidade;
        private readonly ILogService _logService;

        public ConfirmarOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            IServicoDeDisponibilidadeDeEstoque servicoDeDisponibilidade,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _servicoDeDisponibilidade = servicoDeDisponibilidade;
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

        // A DECISÃO de negócio "há saldo suficiente de cada insumo?" cruza os agregados
        // OrdemDeProducao e SaldoDeEstoque e, por isso, vive em um Serviço de Domínio.
        // Ao caso de uso resta a ORQUESTRAÇÃO: traduzir as necessidades da ordem para a
        // entrada neutra do serviço, delegar a verificação e vestir o veredito com o
        // vocabulário deste contexto (insumo).
        private async Task<Resultado<bool>> ValidarDisponibilidadeDeEstoqueAsync(
            OrdemDeProducao ordem,
            CancellationToken cancellationToken)
        {
            var requisicoes = ordem.Necessidades
                .Select(necessidade => new RequisicaoDeDisponibilidade(
                    IdProduto: necessidade.IdInsumo,
                    QuantidadeRequerida: necessidade.QuantidadeNecessaria));

            var verificacao = await _servicoDeDisponibilidade
                .VerificarDisponibilidadeAsync(requisicoes, cancellationToken);

            if (verificacao.EhFalha)
                return Resultado<bool>.Falha(verificacao.Erros!);

            if (verificacao.Instancia.HaDisponibilidade)
                return Resultado<bool>.Sucesso(true);

            var erros = new List<string> { "ESTOQUE_INSUFICIENTE" };
            erros.AddRange(verificacao.Instancia.Insuficiencias
                .Select(insuficiencia =>
                    $"INSUMO_INSUFICIENTE|IdInsumo={insuficiencia.IdProduto}" +
                    $"|Necessario={insuficiencia.QuantidadeRequerida}|Disponivel={insuficiencia.QuantidadeDisponivel}"));

            return Resultado<bool>.Falha(erros);
        }
    }
}
