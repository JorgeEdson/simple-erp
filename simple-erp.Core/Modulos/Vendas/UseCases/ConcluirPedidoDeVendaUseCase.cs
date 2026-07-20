using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IConcluirPedidoDeVendaUseCase
        : IUseCase<ConcluirPedidoDeVendaEntrada, ConcluirPedidoDeVendaSaida>
    {
    }

    public record ConcluirPedidoDeVendaEntrada(long Id);

    public record ConcluirPedidoDeVendaSaida(
        long Id,
        string Status);

    public sealed class ConcluirPedidoDeVendaUseCase : IConcluirPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ConcluirPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ConcluirPedidoDeVendaSaida>> ExecutarAsync(ConcluirPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConcluirPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando conclusão de pedido de venda."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirPedidoDeVendaSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Conclusão do pedido de venda

                var resultadoConcluir = pedido.Concluir();

                if (resultadoConcluir.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao concluir o agregado PedidoDeVenda.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeVendaId"] = pedido.Id.Valor,
                            ["Status"] = pedido.Status.ToString(),
                            ["Erros"] = resultadoConcluir.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<ConcluirPedidoDeVendaSaida>.Falha(resultadoConcluir.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de venda concluído com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConcluirPedidoDeVendaSaida>.Sucesso(
                new ConcluirPedidoDeVendaSaida(
                    Id: pedido.Id.Valor,
                    Status: pedido.Status.ToString()));

            #endregion
        }
    }
}
