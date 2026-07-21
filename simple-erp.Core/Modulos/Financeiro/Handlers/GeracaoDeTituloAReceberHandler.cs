using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.Vendas.Eventos;
using System;

namespace simple_erp.Core.Modulos.Financeiro.Handlers
{   
    public sealed class GeracaoDeTituloAReceberHandler
        : IManipuladorDeEventoDeDominio<PedidoDeVendaAprovado>
    {
        private const int PrazoPadraoDeVencimentoEmDias = 30;

        private readonly IEmitirTituloAReceberUseCase _emitirTituloAReceber;
        private readonly ILogService _logService;

        public GeracaoDeTituloAReceberHandler(
            IEmitirTituloAReceberUseCase emitirTituloAReceber,
            ILogService logService)
        {
            _emitirTituloAReceber = emitirTituloAReceber;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            PedidoDeVendaAprovado evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à aprovação de venda: emitindo título a receber.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(GeracaoDeTituloAReceberHandler),
                    ["PedidoDeVendaId"] = evento.IdPedidoDeVenda.Valor,
                    ["ValorTotal"] = evento.ValorTotal
                }));

            var entrada = new EmitirTituloAReceberEntrada(
                IdCliente: evento.IdCliente.Valor,
                Valor: evento.ValorTotal,
                DataVencimento: DateTime.UtcNow.AddDays(PrazoPadraoDeVencimentoEmDias),
                IdPedidoDeVenda: evento.IdPedidoDeVenda.Valor);

            var resultado = await _emitirTituloAReceber.ExecutarAsync(entrada, cancellationToken);

            if (resultado.EhFalha)
                return Resultado<bool>.Falha(resultado.Erros!);

            return Resultado<bool>.Sucesso(true);
        }
    }
}
