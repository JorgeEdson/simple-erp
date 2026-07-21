using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using System;

namespace simple_erp.Core.Modulos.Financeiro.Handlers
{   
    public sealed class GeracaoDeTituloAPagarHandler
        : IManipuladorDeEventoDeDominio<PedidoDeCompraEfetivado>
    {
        private const int PrazoPadraoDeVencimentoEmDias = 30;

        private readonly IEmitirTituloAPagarUseCase _emitirTituloAPagar;
        private readonly ILogService _logService;

        public GeracaoDeTituloAPagarHandler(
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
                    ["Handler"] = nameof(GeracaoDeTituloAPagarHandler),
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
