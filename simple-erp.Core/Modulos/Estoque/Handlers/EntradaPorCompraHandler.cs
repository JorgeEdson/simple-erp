using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Suprimentos.Eventos;

namespace simple_erp.Core.Modulos.Estoque.Handlers
{   
    public sealed class EntradaPorCompraHandler : IManipuladorDeEventoDeDominio<PedidoDeCompraEfetivado>
    {
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;
        private readonly ILogService _logService;

        public EntradaPorCompraHandler(
            IRegistrarMovimentacaoDeEstoqueUseCase registrarMovimentacao,
            ILogService logService)
        {
            _registrarMovimentacao = registrarMovimentacao;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            PedidoDeCompraEfetivado evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à efetivação de compra: registrando entradas de estoque.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(EntradaPorCompraHandler),
                    ["PedidoDeCompraId"] = evento.IdPedidoDeCompra.Valor,
                    ["QuantidadeItens"] = evento.Itens.Count
                }));

            var erros = new List<string>();

            foreach (var item in evento.Itens)
            {
                var entrada = new RegistrarMovimentacaoDeEstoqueEntrada(
                    IdProduto: item.IdProduto,
                    Tipo: TipoDeMovimentacao.EntradaPorCompra,
                    Quantidade: item.Quantidade,
                    OrigemTipo: TipoOrigemMovimentacao.Compra,
                    OrigemIdReferencia: evento.IdPedidoDeCompra.Valor,
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
