using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IAdicionarItemAoPedidoDeVendaUseCase
        : IUseCase<AdicionarItemAoPedidoDeVendaEntrada, AdicionarItemAoPedidoDeVendaSaida>
    {
    }

    public record AdicionarItemAoPedidoDeVendaEntrada(
        long IdPedidoDeVenda,
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto = 0m);

    public record AdicionarItemAoPedidoDeVendaSaida(
        long Id,
        string Status,
        decimal ValorTotal,
        int QuantidadeItens);

    public sealed class AdicionarItemAoPedidoDeVendaUseCase : IAdicionarItemAoPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AdicionarItemAoPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AdicionarItemAoPedidoDeVendaSaida>> ExecutarAsync(AdicionarItemAoPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AdicionarItemAoPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.IdPedidoDeVenda,
                ["IdProduto"] = dados.IdProduto
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando adição de item ao pedido de venda."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.IdPedidoDeVenda);
            var resultadoIdProduto = Id.TentarCriar(dados.IdProduto);
            var resultadoQuantidade = Quantidade.TentarCriar(dados.Quantidade);
            var resultadoPreco = Dinheiro.TentarCriar(dados.PrecoUnitario);
            var resultadoDesconto = Dinheiro.TentarCriar(dados.Desconto);

            var validacao = Resultado.Combinar(
                resultadoId,
                resultadoIdProduto,
                resultadoQuantidade,
                resultadoPreco,
                resultadoDesconto);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para adição de item ao pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            var produtoExiste = await _unitOfWork.ProdutosRepository.ExistePorIdAsync(
                resultadoIdProduto.Instancia, cancellationToken);

            if (produtoExiste.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(produtoExiste.Erros!);
            }

            if (!produtoExiste.Instancia)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Adição do item ao pedido

                var resultadoItem = ItemDePedidoDeVenda.TentarCriar(
                    resultadoIdProduto.Instancia,
                    resultadoQuantidade.Instancia,
                    resultadoPreco.Instancia,
                    resultadoDesconto.Instancia);

                if (resultadoItem.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(resultadoItem.Erros!);
                }

                var resultadoAdicao = pedido.AdicionarItem(resultadoItem.Instancia);

                if (resultadoAdicao.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao adicionar item ao agregado PedidoDeVenda.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeVendaId"] = pedido.Id.Valor,
                            ["Erros"] = resultadoAdicao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(resultadoAdicao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Item adicionado ao pedido de venda com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["QuantidadeItens"] = pedido.Itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AdicionarItemAoPedidoDeVendaSaida>.Sucesso(
                new AdicionarItemAoPedidoDeVendaSaida(
                    Id: pedido.Id.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    QuantidadeItens: pedido.Itens.Count));

            #endregion
        }
    }
}
