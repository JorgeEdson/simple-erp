using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IObterPedidoDeVendaPorIdUseCase
        : IUseCase<ObterPedidoDeVendaPorIdEntrada, ObterPedidoDeVendaPorIdSaida>
    {
    }

    public record ObterPedidoDeVendaPorIdEntrada(Guid Id) : IRequisicao<ObterPedidoDeVendaPorIdSaida>;

    public record ObterPedidoDeVendaPorIdItemSaida(
        Guid IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto,
        decimal Subtotal);

    public record ObterPedidoDeVendaPorIdSaida(
        Guid Id,
        int Numero,
        Guid IdCliente,
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

                return Resultado<ObterPedidoDeVendaPorIdSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

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
                    ["PedidoDeVendaId"] = pedido.Id,
                    ["Status"] = pedido.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterPedidoDeVendaPorIdSaida>.Sucesso(
                new ObterPedidoDeVendaPorIdSaida(
                    Id: pedido.Id,
                    Numero: pedido.Numero,
                    IdCliente: pedido.IdCliente,
                    Status: pedido.Status.ToString(),
                    DescontoDoPedido: pedido.DescontoDoPedido,
                    ValorTotal: pedido.ValorTotal.Valor,
                    MotivoCancelamento: pedido.MotivoCancelamento,
                    Itens: itens));

            #endregion
        }
    }
}
