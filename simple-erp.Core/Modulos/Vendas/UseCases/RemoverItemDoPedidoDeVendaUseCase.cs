using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IRemoverItemDoPedidoDeVendaUseCase
        : IUseCase<RemoverItemDoPedidoDeVendaEntrada, RemoverItemDoPedidoDeVendaSaida>
    {
    }

    public record RemoverItemDoPedidoDeVendaEntrada(
        Guid IdPedidoDeVenda,
        Guid IdProduto) : IRequisicao<RemoverItemDoPedidoDeVendaSaida>;

    public record RemoverItemDoPedidoDeVendaSaida(
        Guid Id,
        string Status,
        decimal ValorTotal,
        int QuantidadeItens);

    public sealed class RemoverItemDoPedidoDeVendaUseCase : IRemoverItemDoPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public RemoverItemDoPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<RemoverItemDoPedidoDeVendaSaida>> ExecutarAsync(RemoverItemDoPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(RemoverItemDoPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.IdPedidoDeVenda,
                ["IdProduto"] = dados.IdProduto
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando remoção de item do pedido de venda."));

            #endregion

            #region Validação do identificador

            if (dados.IdPedidoDeVenda == Guid.Empty
                || dados.IdProduto == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdPedidoDeVenda"] = dados.IdPedidoDeVenda,
                        ["IdProduto"] = dados.IdProduto,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(dados.IdPedidoDeVenda, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Remoção do item do pedido

                var resultadoRemocao = pedido.RemoverItem(dados.IdProduto);

                if (resultadoRemocao.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao remover item do agregado PedidoDeVenda.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeVendaId"] = pedido.Id,
                            ["Erros"] = resultadoRemocao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha(resultadoRemocao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir remoção de item de pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<RemoverItemDoPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Item removido do pedido de venda com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id,
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["QuantidadeItens"] = pedido.Itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<RemoverItemDoPedidoDeVendaSaida>.Sucesso(
                new RemoverItemDoPedidoDeVendaSaida(
                    Id: pedido.Id,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    QuantidadeItens: pedido.Itens.Count));

            #endregion
        }
    }
}
