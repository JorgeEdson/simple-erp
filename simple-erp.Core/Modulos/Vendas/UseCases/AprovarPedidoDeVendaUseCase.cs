using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IAprovarPedidoDeVendaUseCase
        : IUseCase<AprovarPedidoDeVendaEntrada, AprovarPedidoDeVendaSaida>
    {
    }

    public record AprovarPedidoDeVendaEntrada(long Id);

    public record AprovarPedidoDeVendaSaida(
        long Id,
        string Status,
        decimal ValorTotal);

    public sealed class AprovarPedidoDeVendaUseCase : IAprovarPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public AprovarPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<AprovarPedidoDeVendaSaida>> ExecutarAsync(AprovarPedidoDeVendaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(AprovarPedidoDeVendaUseCase),
                ["PedidoDeVendaId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando aprovação de pedido de venda."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoPedido.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(resultadoPedido.Erros!);
            }

            var pedido = resultadoPedido.Instancia;

            if (pedido is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de aprovar pedido de venda não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AprovarPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            if (pedido.EstaAprovado)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Sucesso(
                    new AprovarPedidoDeVendaSaida(
                        Id: pedido.Id.Valor,
                        Status: pedido.Status.ToString(),
                        ValorTotal: pedido.ValorTotal.Valor));
            }

            if (!pedido.EstaEmEdicao)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_NAO_PODE_SER_APROVADO");
            }

            if (!pedido.PossuiItens)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha("PEDIDO_DE_VENDA_SEM_ITENS");
            }

            #region Validação de disponibilidade de estoque

            var insuficientes = new List<string>();
            var errosDisponibilidade = new List<string>();

            foreach (var item in pedido.Itens)
            {
                var resultadoIdProduto = Id.TentarCriar(item.IdProduto);

                if (resultadoIdProduto.EhFalha)
                {
                    errosDisponibilidade.AddRange(resultadoIdProduto.Erros!);
                    break;
                }

                var disponivel = 0m;

                var existeSaldo = await _unitOfWork.SaldosDeEstoqueRepository
                    .ExistePorProdutoAsync(resultadoIdProduto.Instancia, cancellationToken);

                if (existeSaldo.EhFalha)
                {
                    errosDisponibilidade.AddRange(existeSaldo.Erros!);
                    break;
                }

                if (existeSaldo.Instancia)
                {
                    var resultadoSaldo = await _unitOfWork.SaldosDeEstoqueRepository
                        .ObterPorProdutoAsync(resultadoIdProduto.Instancia, cancellationToken);

                    if (resultadoSaldo.EhFalha)
                    {
                        errosDisponibilidade.AddRange(resultadoSaldo.Erros!);
                        break;
                    }

                    disponivel = resultadoSaldo.Instancia!.QuantidadeAtual;
                }

                if (disponivel < item.Quantidade)
                {
                    insuficientes.Add(
                        $"PRODUTO_INSUFICIENTE|IdProduto={item.IdProduto}" +
                        $"|Necessario={item.Quantidade}|Disponivel={disponivel}");
                }
            }

            if (errosDisponibilidade.Count > 0)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Aprovação bloqueada por falha ao checar disponibilidade de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = pedido.Id.Valor,
                        ["Erros"] = errosDisponibilidade.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(errosDisponibilidade);
            }

            if (insuficientes.Count > 0)
            {
                var errosFinal = new List<string> { "ESTOQUE_INSUFICIENTE" };
                errosFinal.AddRange(insuficientes);

                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Aprovação bloqueada por indisponibilidade de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = pedido.Id.Valor,
                        ["Erros"] = errosFinal.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(errosFinal);
            }

            #endregion

            #endregion

            #region Execução das regras de negócio

            #region Aprovação do pedido de venda

            var resultadoAprovar = pedido.Aprovar();

            if (resultadoAprovar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(resultadoAprovar.Erros!);
            }

            #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.PedidosDeVendaRepository
                .AtualizarAsync(pedido, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir aprovação de pedido de venda.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            // Os eventos de domínio produzidos por este agregado NÃO são despachados
            // aqui. O interceptor de persistência os gravou na caixa de saída dentro da
            // mesma transação do SaveChanges acima, e o worker que consome o outbox os
            // entrega aos manipuladores fora desta requisição.
            //
            // Duas consequências que valem ser ditas em voz alta: a resposta ao usuário
            // não espera pelos efeitos em outros contextos delimitados, e nenhum efeito
            // se perde caso a aplicação caia logo após a confirmação.

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Pedido de venda aprovado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["PedidoDeVendaId"] = pedido.Id.Valor,
                    ["Status"] = pedido.Status.ToString(),
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AprovarPedidoDeVendaSaida>.Sucesso(
                new AprovarPedidoDeVendaSaida(
                    Id: pedido.Id.Valor,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor));

            #endregion
        }
    }
}