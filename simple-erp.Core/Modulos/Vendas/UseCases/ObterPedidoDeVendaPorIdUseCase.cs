using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IObterPedidoDeVendaPorIdUseCase
        : IUseCase<ObterPedidoDeVendaPorIdEntrada, ObterPedidoDeVendaPorIdSaida>
    {
    }

    public record ObterPedidoDeVendaPorIdEntrada(long Id);

    public record ObterPedidoDeVendaPorIdItemSaida(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto,
        decimal Subtotal);

    public record ObterPedidoDeVendaPorIdSaida(
        long Id,
        int Numero,
        long IdCliente,
        string Status,
        decimal DescontoDoPedido,
        decimal ValorTotal,
        string? MotivoCancelamento,
        IReadOnlyCollection<ObterPedidoDeVendaPorIdItemSaida> Itens);

    public sealed class ObterPedidoDeVendaPorIdUseCase : IObterPedidoDeVendaPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterPedidoDeVendaPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterPedidoDeVendaPorIdSaida>> ExecutarAsync(ObterPedidoDeVendaPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterPedidoDeVendaPorIdUseCase),
                ["PedidoDeVendaId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta de pedido de venda por id."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterPedidoDeVendaPorIdSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterPedidoDeVendaPorIdSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Pedido de venda não encontrado na consulta por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ObterPedidoDeVendaPorIdSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var itens = pedido.Itens
                .Select(item => new ObterPedidoDeVendaPorIdItemSaida(
                    IdProduto: item.IdProduto,
                    Quantidade: item.Quantidade,
                    PrecoUnitario: item.PrecoUnitario,
                    Desconto: item.Desconto,
                    Subtotal: item.Subtotal))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta de pedido de venda por id concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterPedidoDeVendaPorIdSaida>.Sucesso(
                new ObterPedidoDeVendaPorIdSaida(
                    Id: pedido.Id.Valor,
                    Numero: pedido.Numero,
                    IdCliente: pedido.IdCliente.Valor,
                    Status: pedido.Status.ToString(),
                    DescontoDoPedido: pedido.DescontoDoPedido,
                    ValorTotal: pedido.ValorTotal.Valor,
                    MotivoCancelamento: pedido.MotivoCancelamento,
                    Itens: itens));

            #endregion
        }
    }
}
