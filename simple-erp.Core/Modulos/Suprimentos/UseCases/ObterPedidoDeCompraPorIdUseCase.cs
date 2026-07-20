using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Suprimentos.UseCases
{
    public interface IObterPedidoDeCompraPorIdUseCase
        : IUseCase<ObterPedidoDeCompraPorIdEntrada, ObterPedidoDeCompraPorIdSaida>
    {
    }

    public record ObterPedidoDeCompraPorIdEntrada(long Id);

    public record ObterPedidoDeCompraPorIdItemSaida(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario,
        decimal Subtotal);

    public record ObterPedidoDeCompraPorIdSaida(
        long Id,
        long IdFornecedor,
        string Status,
        decimal ValorTotal,
        IReadOnlyCollection<ObterPedidoDeCompraPorIdItemSaida> Itens);

    public sealed class ObterPedidoDeCompraPorIdUseCase : IObterPedidoDeCompraPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterPedidoDeCompraPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterPedidoDeCompraPorIdSaida>> ExecutarAsync(ObterPedidoDeCompraPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterPedidoDeCompraPorIdUseCase),
                ["PedidoDeCompraId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta de pedido de compra por id."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para consulta de pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterPedidoDeCompraPorIdSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObter = Stopwatch.StartNew();

            var resultadoPedido = await _unitOfWork.PedidosDeCompraRepository.ObterPorIdAsync(
                resultadoId.Instancia,
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
                    Mensagem: "Falha ao obter pedido de compra por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPedido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterPedidoDeCompraPorIdSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Pedido de compra não encontrado na consulta por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterPedidoDeCompraPorIdSaida>.Falha("PEDIDO_DE_COMPRA_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var itens = pedido.Itens
                .Select(item => new ObterPedidoDeCompraPorIdItemSaida(
                    IdProduto: item.IdProduto,
                    Quantidade: item.Quantidade,
                    CustoUnitario: item.CustoUnitario,
                    Subtotal: item.Subtotal))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta de pedido de compra por id concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeCompraId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterPedidoDeCompraPorIdSaida>.Sucesso(
                new ObterPedidoDeCompraPorIdSaida(
                    Id: pedido.Id.Valor,
                    IdFornecedor: pedido.IdFornecedor.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    Itens: itens));

            #endregion
        }
    }
}
