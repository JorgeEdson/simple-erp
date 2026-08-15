using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Modulos.Estoque.Servicos;
using System.Diagnostics;
using System.Linq;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.Vendas.UseCases
{
    public interface IAprovarPedidoDeVendaUseCase
        : IUseCase<AprovarPedidoDeVendaEntrada, AprovarPedidoDeVendaSaida>
    {
    }

    public record AprovarPedidoDeVendaEntrada(Guid Id) : IRequisicao<AprovarPedidoDeVendaSaida>;

    public record AprovarPedidoDeVendaSaida(
        Guid Id,
        string Status,
        decimal ValorTotal);

    public sealed class AprovarPedidoDeVendaUseCase : IAprovarPedidoDeVendaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServicoDeDisponibilidadeDeEstoque _servicoDeDisponibilidade;
        private readonly ILogService _logService;

        public AprovarPedidoDeVendaUseCase(
            IUnitOfWork unitOfWork,
            IServicoDeDisponibilidadeDeEstoque servicoDeDisponibilidade,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _servicoDeDisponibilidade = servicoDeDisponibilidade;
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

                return Resultado<AprovarPedidoDeVendaSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var resultadoPedido = await _unitOfWork.PedidosDeVendaRepository
                .ObterPorIdAsync(dados.Id, cancellationToken);

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
                        Id: pedido.Id,
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

            // Mesma DECISÃO cross-agregado da Produção, reaproveitada aqui: o serviço de
            // domínio apura a insuficiência; este caso de uso apenas veste o veredito com
            // o vocabulário de Vendas (produto).
            var requisicoes = pedido.Itens
                .Select(item => new RequisicaoDeDisponibilidade(
                    IdProduto: item.IdProduto,
                    QuantidadeRequerida: item.Quantidade));

            var verificacao = await _servicoDeDisponibilidade
                .VerificarDisponibilidadeAsync(requisicoes, cancellationToken);

            if (verificacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Aprovação bloqueada por falha ao checar disponibilidade de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = pedido.Id,
                        ["Erros"] = verificacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<AprovarPedidoDeVendaSaida>.Falha(verificacao.Erros!);
            }

            if (!verificacao.Instancia.HaDisponibilidade)
            {
                var errosFinal = new List<string> { "ESTOQUE_INSUFICIENTE" };
                errosFinal.AddRange(verificacao.Instancia.Insuficiencias
                    .Select(insuficiencia =>
                        $"PRODUTO_INSUFICIENTE|IdProduto={insuficiencia.IdProduto}" +
                        $"|Necessario={insuficiencia.QuantidadeRequerida}|Disponivel={insuficiencia.QuantidadeDisponivel}"));

                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Aprovação bloqueada por indisponibilidade de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["PedidoDeVendaId"] = pedido.Id,
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
                    ["PedidoDeVendaId"] = pedido.Id,
                    ["Status"] = pedido.Status.ToString(),
                    ["ValorTotal"] = pedido.ValorTotal.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<AprovarPedidoDeVendaSaida>.Sucesso(
                new AprovarPedidoDeVendaSaida(
                    Id: pedido.Id,
                    Status: pedido.Status.ToString(),
                    ValorTotal: pedido.ValorTotal.Valor));

            #endregion
        }
    }
}