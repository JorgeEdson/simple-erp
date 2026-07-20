using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using System;

namespace simple_erp.Core.Modulos.Financeiro.Handlers
{
    /// <summary>
    /// Handler de integração: quando uma compra é efetivada (evento do contexto de
    /// Suprimentos), o Financeiro reage emitindo um TÍTULO A PAGAR para o fornecedor,
    /// no valor total do pedido e com um prazo de vencimento padrão.
    ///
    /// Esta é a "rota A" da decisão de negócio (gerar o título na efetivação). A "rota B"
    /// alternativa seria reagir a PedidoDeCompraAprovado.
    /// </summary>
    public sealed class ManipuladorGeracaoDeTituloAPagar
        : IManipuladorDeEventoDeDominio<PedidoDeCompraEfetivado>
    {
        private const int PrazoPadraoDeVencimentoEmDias = 30;

        private readonly IEmitirTituloAPagarUseCase _emitirTituloAPagar;
        private readonly ILogService _logService;

        public ManipuladorGeracaoDeTituloAPagar(
            IEmitirTituloAPagarUseCase emitirTituloAPagar,
            ILogService logService)
        {
            _emitirTituloAPagar = emitirTituloAPagar;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            PedidoDeCompraEfetivado evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à efetivação de compra: emitindo título a pagar.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(ManipuladorGeracaoDeTituloAPagar),
                    ["PedidoDeCompraId"] = evento.IdPedidoDeCompra.Valor,
                    ["ValorTotal"] = evento.ValorTotal
                }));

            var entrada = new EmitirTituloAPagarEntrada(
                IdFornecedor: evento.IdFornecedor.Valor,
                Valor: evento.ValorTotal,
                DataVencimento: DateTime.UtcNow.AddDays(PrazoPadraoDeVencimentoEmDias),
                IdPedidoDeCompra: evento.IdPedidoDeCompra.Valor);

            var resultado = await _emitirTituloAPagar.ExecutarAsync(entrada, cancellationToken);

            if (resultado.EhFalha)
                return Resultado<bool>.Falha(resultado.Erros!);

            return Resultado<bool>.Sucesso(true);
        }
    }
}
