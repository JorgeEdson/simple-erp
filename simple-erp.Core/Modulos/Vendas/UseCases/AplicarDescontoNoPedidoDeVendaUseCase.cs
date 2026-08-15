using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IAplicarDescontoNoPedidoDeVendaUseCase
        : IUseCase<AplicarDescontoNoPedidoDeVendaEntrada, AplicarDescontoNoPedidoDeVendaSaida>
    {
    }

    public record AplicarDescontoNoPedidoDeVendaEntrada(
        Guid IdPedidoDeVenda,
        decimal Desconto) : IRequisicao<AplicarDescontoNoPedidoDeVendaSaida>;

    public record AplicarDescontoNoPedidoDeVendaSaida(
        Guid Id,
        string Status,
        decimal DescontoDoPedido,
        decimal ValorTotal);

    public sealed class AplicarDescontoNoPedidoDeVendaUseCase : IAplicarDescontoNoPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AplicarDescontoNoPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AplicarDescontoNoPedidoDeVendaSaida>> ExecutarAsync(AplicarDescontoNoPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AplicarDescontoNoPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.IdPedidoDeVenda,
                ["Desconto"] = dados.Desconto
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando aplicação de desconto no pedido de venda."));

            #endregion

            #region Validação do identificador

            if (dados.IdPedidoDeVenda == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdPedidoDeVenda"] = dados.IdPedidoDeVenda,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Validação da entrada

            var resultadoDesconto = Dinheiro.TentarCriar(dados.Desconto);

            var validacao = Resultado.Combinar(resultadoDesconto);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(dados.IdPedidoDeVenda, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Aplicação do desconto no pedido

                var resultadoAplicacao = pedido.AplicarDescontoNoPedido(resultadoDesconto.Instancia);

                if (resultadoAplicacao.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao aplicar desconto no agregado PedidoDeVenda.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeVendaId"] = pedido.Id,
                            ["Erros"] = resultadoAplicacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha(resultadoAplicacao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Desconto aplicado no pedido de venda com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id,
                    ["DescontoDoPedido"] = pedido.DescontoDoPedido,
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AplicarDescontoNoPedidoDeVendaSaida>.Sucesso(
                new AplicarDescontoNoPedidoDeVendaSaida(
                    Id: pedido.Id,
                    Status: pedido.Status.ToString(),
                    DescontoDoPedido: pedido.DescontoDoPedido,
                    ValorTotal: pedido.ValorTotal.Valor));

            #endregion
        }
    }
}
