using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface ICancelarPedidoDeVendaUseCase
        : IUseCase<CancelarPedidoDeVendaEntrada, CancelarPedidoDeVendaSaida>
    {
    }

    public record CancelarPedidoDeVendaEntrada(long Id, string Motivo);

    public record CancelarPedidoDeVendaSaida(
        long Id,
        string Status,
        string Motivo);

    public sealed class CancelarPedidoDeVendaUseCase : ICancelarPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CancelarPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CancelarPedidoDeVendaSaida>> ExecutarAsync(CancelarPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CancelarPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cancelamento de pedido de venda."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);
            var resultadoMotivo = MotivoCancelamento.TentarCriar(dados.Motivo);

            var validacao = Resultado.Combinar(resultadoId, resultadoMotivo);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para cancelamento de pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CancelarPedidoDeVendaSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Cancelamento do pedido de venda

                var resultadoCancelamento = pedido.Cancelar(resultadoMotivo.Instancia);

                if (resultadoCancelamento.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao cancelar o agregado PedidoDeVenda.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeVendaId"] = pedido.Id.Valor,
                            ["Status"] = pedido.Status.ToString(),
                            ["Erros"] = resultadoCancelamento.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<CancelarPedidoDeVendaSaida>.Falha(resultadoCancelamento.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CancelarPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de venda cancelado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CancelarPedidoDeVendaSaida>.Sucesso(
                new CancelarPedidoDeVendaSaida(
                    Id: pedido.Id.Valor,
                    Status: pedido.Status.ToString(),
                    Motivo: pedido.MotivoCancelamento ?? dados.Motivo));

            #endregion
        }
    }
}
