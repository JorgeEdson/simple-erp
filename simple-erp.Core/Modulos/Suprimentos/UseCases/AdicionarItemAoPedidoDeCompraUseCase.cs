using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Suprimentos.UseCases
{
    public interface IAdicionarItemAoPedidoDeCompraUseCase
        : IUseCase<AdicionarItemAoPedidoDeCompraEntrada, AdicionarItemAoPedidoDeCompraSaida>
    {
    }

    public record AdicionarItemAoPedidoDeCompraEntrada(
        long IdPedidoDeCompra,
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);

    public record AdicionarItemAoPedidoDeCompraSaida(
        long Id,
        string Status,
        decimal ValorTotal,
        int QuantidadeItens);

    public sealed class AdicionarItemAoPedidoDeCompraUseCase : IAdicionarItemAoPedidoDeCompraUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AdicionarItemAoPedidoDeCompraUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AdicionarItemAoPedidoDeCompraSaida>> ExecutarAsync(AdicionarItemAoPedidoDeCompraEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AdicionarItemAoPedidoDeCompraUseCase),
                ["PedidoDeCompraId"] = dados.IdPedidoDeCompra,
                ["IdProduto"] = dados.IdProduto
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando adição de item ao pedido de compra."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.IdPedidoDeCompra);
            var resultadoIdProduto = Id.TentarCriar(dados.IdProduto);
            var resultadoQuantidade = Quantidade.TentarCriar(dados.Quantidade);
            var resultadoCusto = Dinheiro.TentarCriar(dados.CustoUnitario);

            var validacao = Resultado.Combinar(
                resultadoId,
                resultadoIdProduto,
                resultadoQuantidade,
                resultadoCusto);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para adição de item ao pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(validacao.Erros!);
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
                    Mensagem: "Falha ao obter pedido de compra por id para adição de item.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPedido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de adicionar item a pedido de compra não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = dados.IdPedidoDeCompra,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha("PEDIDO_DE_COMPRA_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            var produtoExiste = await _unitOfWork.ProdutosRepository.ExistePorIdAsync(
                resultadoIdProduto.Instancia,
                cancellationToken);

            if (produtoExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência do produto para adição de item.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = produtoExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(produtoExiste.Erros!);
            }

            if (!produtoExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de adicionar item com produto inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProduto"] = dados.IdProduto,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Adição do item ao pedido

                var resultadoItem = ItemDePedidoDeCompra.TentarCriar(
                    resultadoIdProduto.Instancia,
                    resultadoQuantidade.Instancia,
                    resultadoCusto.Instancia);

                if (resultadoItem.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao criar item de pedido de compra.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoItem.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(resultadoItem.Erros!);
                }

                var resultadoAdicao = pedido.AdicionarItem(resultadoItem.Instancia);

                if (resultadoAdicao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao adicionar item ao agregado PedidoDeCompra.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["PedidoDeCompraId"] = pedido.Id.Valor,
                            ["Erros"] = resultadoAdicao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(resultadoAdicao.Erros!);
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
                    Mensagem: "Falha ao atualizar pedido de compra no repositório durante adição de item.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = pedido.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSave = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSave.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da adição de item de pedido de compra concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSave.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir adição de item de pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = pedido.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Item adicionado ao pedido de compra com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeCompraId"] = pedido.Id.Valor,
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["QuantidadeItens"] = pedido.Itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AdicionarItemAoPedidoDeCompraSaida>.Sucesso(
                new AdicionarItemAoPedidoDeCompraSaida(
                    Id: pedido.Id.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    QuantidadeItens: pedido.Itens.Count));

            #endregion
        }
    }
}
