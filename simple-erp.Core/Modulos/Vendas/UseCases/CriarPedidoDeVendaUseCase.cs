using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface ICriarPedidoDeVendaUseCase
        : IUseCase<CriarPedidoDeVendaEntrada, CriarPedidoDeVendaSaida>
    {
    }

    public record ItemPedidoDeVendaEntrada(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto = 0m);

    public record CriarPedidoDeVendaEntrada(
        long IdCliente,
        IReadOnlyCollection<ItemPedidoDeVendaEntrada> Itens,
        decimal DescontoDoPedido = 0m) : IRequisicao<CriarPedidoDeVendaSaida>;

    public record ItemPedidoDeVendaSaida(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto,
        decimal Subtotal);

    public record CriarPedidoDeVendaSaida(
        long Id,
        int Numero,
        long IdCliente,
        string Status,
        decimal ValorTotal,
        IReadOnlyCollection<ItemPedidoDeVendaSaida> Itens);

    public sealed class CriarPedidoDeVendaUseCase : ICriarPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CriarPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CriarPedidoDeVendaSaida>> ExecutarAsync(CriarPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CriarPedidoDeVendaUseCase),
                ["IdCliente"] = dados.IdCliente,
                ["QuantidadeItens"] = dados.Itens?.Count ?? 0
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando criação de pedido de venda."));

            #endregion

            #region Validação da entrada

            var resultadoIdCliente = Id.TentarCriar(dados.IdCliente);
            var resultadoDesconto = Dinheiro.TentarCriar(dados.DescontoDoPedido);

            var validacaoCampos = Resultado.Combinar(resultadoIdCliente, resultadoDesconto);

            if (validacaoCampos.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para criação de pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarPedidoDeVendaSaida>.Falha(validacaoCampos.Erros!);
            }

            #region Conversão e Validação dos Itens

            var itens = new List<ItemDePedidoDeVenda>();
            var errosItens = new List<string>();

            if (dados.Itens is not null && dados.Itens.Count > 0)
            {
                foreach (var itemEntrada in dados.Itens)
                {
                    var resultadoIdProduto = Id.TentarCriar(itemEntrada.IdProduto);
                    var resultadoQuantidade = Quantidade.TentarCriar(itemEntrada.Quantidade);
                    var resultadoPreco = Dinheiro.TentarCriar(itemEntrada.PrecoUnitario);
                    var resultadoDescontoItem = Dinheiro.TentarCriar(itemEntrada.Desconto);

                    var validacaoItem = Resultado.Combinar(
                        resultadoIdProduto,
                        resultadoQuantidade,
                        resultadoPreco,
                        resultadoDescontoItem);

                    if (validacaoItem.EhFalha)
                    {
                        errosItens.AddRange(validacaoItem.Erros!);
                        continue;
                    }

                    var resultadoItem = ItemDePedidoDeVenda.TentarCriar(
                        resultadoIdProduto.Instancia,
                        resultadoQuantidade.Instancia,
                        resultadoPreco.Instancia,
                        resultadoDescontoItem.Instancia);

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
                    Mensagem: "Falha na validação dos itens do pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = errosItens.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarPedidoDeVendaSaida>.Falha(errosItens);
            }

            #endregion

            #endregion

            #region Validação de pré-condições

            var clienteExiste = await _unitOfWork.ClientesRepository.ExistePorIdAsync(
                resultadoIdCliente.Instancia, cancellationToken);

            if (clienteExiste.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CriarPedidoDeVendaSaida>.Falha(clienteExiste.Erros!);
            }

            if (!clienteExiste.Instancia)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de criar pedido de venda para cliente inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdCliente"] = dados.IdCliente,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarPedidoDeVendaSaida>.Falha("CLIENTE_NAO_ENCONTRADO");
            }

            #region Validação da existência dos produtos

            var idsProdutos = itens.Select(item => item.IdProduto).Distinct().ToList();

            foreach (var idProduto in idsProdutos)
            {
                var resultadoIdProd = Id.TentarCriar(idProduto);

                if (resultadoIdProd.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarPedidoDeVendaSaida>.Falha(resultadoIdProd.Erros!);
                }

                var existeProduto = await _unitOfWork.ProdutosRepository.ExistePorIdAsync(
                    resultadoIdProd.Instancia, cancellationToken);

                if (existeProduto.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarPedidoDeVendaSaida>.Falha(existeProduto.Erros!);
                }

                if (!existeProduto.Instancia)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarPedidoDeVendaSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
                }
            }

            #endregion

            #endregion

            #region Execução das regras de negócio

            #region Criação do pedido de venda

            var resultadoNumero = await _unitOfWork.PedidosDeVendaRepository
                .ObterProximoNumeroAsync(cancellationToken);

            if (resultadoNumero.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CriarPedidoDeVendaSaida>.Falha(resultadoNumero.Erros!);
            }

            var resultadoPedido = PedidoDeVenda.Criar(
                resultadoNumero.Instancia,
                resultadoIdCliente.Instancia,
                itens,
                resultadoDesconto.Instancia);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha ao criar o agregado PedidoDeVenda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPedido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            #endregion

            #endregion

            #region Persistência

            await _unitOfWork.PedidosDeVendaRepository.AdicionarAsync(pedido, cancellationToken);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir criação de pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de venda criado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["Numero"] = pedido.Numero,
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            var itensSaida = pedido.Itens
                .Select(item => new ItemPedidoDeVendaSaida(
                    IdProduto: item.IdProduto,
                    Quantidade: item.Quantidade,
                    PrecoUnitario: item.PrecoUnitario,
                    Desconto: item.Desconto,
                    Subtotal: item.Subtotal))
                .ToList();

            return Resultado<CriarPedidoDeVendaSaida>.Sucesso(
                new CriarPedidoDeVendaSaida(
                    Id: pedido.Id.Valor,
                    Numero: pedido.Numero,
                    IdCliente: pedido.IdCliente.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor,
                    Itens: itensSaida));

            #endregion
        }
    }
}