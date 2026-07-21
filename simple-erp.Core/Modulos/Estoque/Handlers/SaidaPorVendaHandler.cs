using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Vendas.Eventos;

namespace simple_erp.Core.Modulos.Estoque.Handlers
{
    
    public sealed class SaidaPorVendaHandler
        : IManipuladorDeEventoDeDominio<PedidoDeVendaAprovado>
    {
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;
        private readonly ILogService _logService;

        public SaidaPorVendaHandler(
            IRegistrarMovimentacaoDeEstoqueUseCase registrarMovimentacao,
            ILogService logService)
        {
            _registrarMovimentacao = registrarMovimentacao;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            PedidoDeVendaAprovado evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à aprovação de venda: registrando saídas de estoque.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(SaidaPorVendaHandler),
                    ["PedidoDeVendaId"] = evento.IdPedidoDeVenda.Valor,
                    ["QuantidadeItens"] = evento.Itens.Count
                }));

            var erros = new List<string>();

            foreach (var item in evento.Itens)
            {
                var entrada = new RegistrarMovimentacaoDeEstoqueEntrada(
                    IdProduto: item.IdProduto,
                    Tipo: TipoDeMovimentacao.SaidaPorVenda,
                    Quantidade: item.Quantidade,
                    OrigemTipo: TipoOrigemMovimentacao.Venda,
                    OrigemIdReferencia: evento.IdPedidoDeVenda.Valor,
                    PermitirSaldoNegativo: false);

                var resultado = await _registrarMovimentacao.ExecutarAsync(entrada, cancellationToken);

                if (resultado.EhFalha)
                    erros.AddRange(resultado.Erros!);
            }

            if (erros.Count > 0)
                return Resultado<bool>.Falha(erros);

            return Resultado<bool>.Sucesso(true);
        }
    }
}
