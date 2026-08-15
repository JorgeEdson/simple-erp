using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Suprimentos.UseCases
{
    public interface IAprovarPedidoDeCompraUseCase
        : IUseCase<AprovarPedidoDeCompraEntrada, AprovarPedidoDeCompraSaida>
    {
    }

    public record AprovarPedidoDeCompraEntrada(Guid Id) : IRequisicao<AprovarPedidoDeCompraSaida>;

    public record AprovarPedidoDeCompraSaida(
        Guid Id,
        string Status,
        decimal ValorTotal);

    public sealed class AprovarPedidoDeCompraUseCase : IAprovarPedidoDeCompraUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AprovarPedidoDeCompraUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AprovarPedidoDeCompraSaida>> ExecutarAsync(AprovarPedidoDeCompraEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AprovarPedidoDeCompraUseCase),
                ["PedidoDeCompraId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando aprovação de pedido de compra."));

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

                return Resultado<AprovarPedidoDeCompraSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObter = Stopwatch.StartNew();

            var resultadoPedido = await _unitOfWork.PedidosDeCompraRepository.ObterPorIdAsync(
                dados.Id,
                cancellationToken);

            stopwatchObter.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de pedido de compra por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObter.ElapsedMilliseconds
                }));

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter pedido de compra por id para aprovação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPedido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AprovarPedidoDeCompraSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de aprovação de pedido de compra não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AprovarPedidoDeCompraSaida>.Falha("PEDIDO_DE_COMPRA_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Aprovação do pedido de compra

                var resultadoAprovacao = pedido.Aprovar();

                if (resultadoAprovacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao aprovar agregado PedidoDeCompra.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeCompraId"] = pedido.Id,
                            ["Status"] = pedido.Status.ToString(),
                            ["Erros"] = resultadoAprovacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<AprovarPedidoDeCompraSaida>.Falha(resultadoAprovacao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.PedidosDeCompraRepository.AtualizarAsync(
                pedido,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de pedido de compra no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar pedido de compra no repositório durante aprovação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = pedido.Id,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AprovarPedidoDeCompraSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSave = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSave.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da aprovação de pedido de compra concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSave.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir aprovação de pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = pedido.Id,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AprovarPedidoDeCompraSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de compra aprovado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeCompraId"] = pedido.Id,
                    ["Status"] = pedido.Status.ToString(),
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AprovarPedidoDeCompraSaida>.Sucesso(
                new AprovarPedidoDeCompraSaida(
                    Id: pedido.Id,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor));

            #endregion
        }
    }
}
