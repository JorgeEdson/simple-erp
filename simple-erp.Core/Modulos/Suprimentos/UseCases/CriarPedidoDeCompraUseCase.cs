using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Suprimentos.UseCases
{
    public interface ICriarPedidoDeCompraUseCase
        : IUseCase<CriarPedidoDeCompraEntrada, CriarPedidoDeCompraSaida>
    {
    }

    public record ItemPedidoDeCompraEntrada(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);

    public record CriarPedidoDeCompraEntrada(
        long IdFornecedor,
        IReadOnlyCollection<ItemPedidoDeCompraEntrada> Itens);

    public record ItemPedidoDeCompraSaida(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario,
        decimal Subtotal);

    public record CriarPedidoDeCompraSaida(
        long Id,
        long IdFornecedor,
        string Status,
        decimal ValorTotal,
        IReadOnlyCollection<ItemPedidoDeCompraSaida> Itens);

    public sealed class CriarPedidoDeCompraUseCase : ICriarPedidoDeCompraUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CriarPedidoDeCompraUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CriarPedidoDeCompraSaida>> ExecutarAsync(CriarPedidoDeCompraEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CriarPedidoDeCompraUseCase),
                ["IdFornecedor"] = dados.IdFornecedor,
                ["QuantidadeItens"] = dados.Itens?.Count ?? 0
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando criação de pedido de compra."));

            #endregion

            #region Validação da entrada

            var resultadoIdFornecedor = Id.TentarCriar(dados.IdFornecedor);

            if (resultadoIdFornecedor.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do fornecedor para criação de pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoIdFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha(resultadoIdFornecedor.Erros!);
            }

            #region Conversão e Validação dos Itens

            var itens = new List<ItemDePedidoDeCompra>();
            var errosItens = new List<string>();

            if (dados.Itens is not null && dados.Itens.Count > 0)
            {
                foreach (var itemEntrada in dados.Itens)
                {
                    var resultadoIdProduto = Id.TentarCriar(itemEntrada.IdProduto);
                    var resultadoQuantidade = Quantidade.TentarCriar(itemEntrada.Quantidade);
                    var resultadoCusto = Dinheiro.TentarCriar(itemEntrada.CustoUnitario);

                    var validacaoItem = Resultado.Combinar(
                        resultadoIdProduto,
                        resultadoQuantidade,
                        resultadoCusto);

                    if (validacaoItem.EhFalha)
                    {
                        errosItens.AddRange(validacaoItem.Erros!);
                        continue;
                    }

                    var resultadoItem = ItemDePedidoDeCompra.TentarCriar(
                        resultadoIdProduto.Instancia,
                        resultadoQuantidade.Instancia,
                        resultadoCusto.Instancia);

                    if (resultadoItem.EhFalha)
                    {
                        errosItens.AddRange(resultadoItem.Erros!);
                        continue;
                    }

                    itens.Add(resultadoItem.Instancia);
                }
            }

            if (errosItens.Count > 0)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos itens do pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = errosItens.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha(errosItens);
            }

            #endregion

            #endregion

            #region Validação de pré-condições

            var stopwatchExisteFornecedor = Stopwatch.StartNew();

            var fornecedorExiste = await _unitOfWork.FornecedoresRepository.ExistePorIdAsync(
                resultadoIdFornecedor.Instancia,
                cancellationToken);

            stopwatchExisteFornecedor.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "FornecedoresRepository.ExistePorIdAsync",
                    ["DuracaoMs"] = stopwatchExisteFornecedor.ElapsedMilliseconds
                }));

            if (fornecedorExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência do fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = fornecedorExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha(fornecedorExiste.Erros!);
            }

            if (!fornecedorExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de criar pedido de compra para fornecedor inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdFornecedor"] = dados.IdFornecedor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            #region Validação da existência dos produtos

            var idsProdutos = itens
                .Select(item => item.IdProduto)
                .Distinct()
                .ToList();

            foreach (var idProduto in idsProdutos)
            {
                var resultadoIdProd = Id.TentarCriar(idProduto);

                if (resultadoIdProd.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarPedidoDeCompraSaida>.Falha(resultadoIdProd.Erros!);
                }

                var existeProduto = await _unitOfWork.ProdutosRepository.ExistePorIdAsync(
                    resultadoIdProd.Instancia,
                    cancellationToken);

                if (existeProduto.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha na verificação de existência dos produtos do pedido de compra.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = existeProduto.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<CriarPedidoDeCompraSaida>.Falha(existeProduto.Erros!);
                }

                if (!existeProduto.Instancia)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarPedidoDeCompraSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
                }
            }

            #endregion

            #endregion

            #region Execução das regras de negócio

            #region Criação do pedido de compra

            var stopwatchCriarAgregado = Stopwatch.StartNew();

            var resultadoPedido = PedidoDeCompra.Criar(
                resultadoIdFornecedor.Instancia,
                itens);

            stopwatchCriarAgregado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Criação do agregado PedidoDeCompra concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "PedidoDeCompra.Criar",
                    ["DuracaoMs"] = stopwatchCriarAgregado.ElapsedMilliseconds
                }));

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao criar agregado PedidoDeCompra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPedido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            #endregion

            #endregion

            #region Persistência

            var stopwatchAdicionar = Stopwatch.StartNew();

            await _unitOfWork.PedidosDeCompraRepository.AdicionarAsync(pedido, cancellationToken);

            stopwatchAdicionar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Adição de pedido de compra no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AdicionarAsync",
                    ["DuracaoMs"] = stopwatchAdicionar.ElapsedMilliseconds
                }));

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da criação de pedido de compra concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir criação de pedido de compra.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeCompraId"] = pedido.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CriarPedidoDeCompraSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de compra criado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeCompraId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            var itensSaida = pedido.Itens
                .Select(item => new ItemPedidoDeCompraSaida(
                    IdProduto: item.IdProduto,
                    Quantidade: item.Quantidade,
                    CustoUnitario: item.CustoUnitario,
                    Subtotal: item.Subtotal))
                .ToList();

            return Resultado<CriarPedidoDeCompraSaida>.Sucesso(
                new CriarPedidoDeCompraSaida(
                    Id: pedido.Id.Valor,
                    IdFornecedor: pedido.IdFornecedor.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    Itens: itensSaida));

            #endregion
        }
    }
}