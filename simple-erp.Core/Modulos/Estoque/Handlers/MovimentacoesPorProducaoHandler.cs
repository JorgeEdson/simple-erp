using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Producao.Eventos;

namespace simple_erp.Core.Modulos.Estoque.Handlers
{   
    public sealed class MovimentacoesPorProducaoHandler
        : IManipuladorDeEventoDeDominio<OrdemDeProducaoConcluida>
    {
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;
        private readonly ILogService _logService;

        public MovimentacoesPorProducaoHandler(
            IRegistrarMovimentacaoDeEstoqueUseCase registrarMovimentacao,
            ILogService logService)
        {
            _registrarMovimentacao = registrarMovimentacao;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            OrdemDeProducaoConcluida evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à conclusão de ordem de produção: baixa de insumos e entrada do acabado.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(MovimentacoesPorProducaoHandler),
                    ["OrdemDeProducaoId"] = evento.IdOrdemDeProducao.Valor,
                    ["IdProdutoFabricado"] = evento.IdProdutoFabricado.Valor,
                    ["QuantidadeInsumos"] = evento.InsumosConsumidos.Count
                }));

            var erros = new List<string>();

            // Saída por produção: uma movimentação por insumo consumido.
            foreach (var insumo in evento.InsumosConsumidos)
            {
                var saida = new RegistrarMovimentacaoDeEstoqueEntrada(
                    IdProduto: insumo.IdInsumo,
                    Tipo: TipoDeMovimentacao.SaidaPorProducao,
                    Quantidade: insumo.Quantidade,
                    OrigemTipo: TipoOrigemMovimentacao.Producao,
                    OrigemIdReferencia: evento.IdOrdemDeProducao.Valor,
                    PermitirSaldoNegativo: false);

                var resultadoSaida = await _registrarMovimentacao.ExecutarAsync(saida, cancellationToken);

                if (resultadoSaida.EhFalha)
                    erros.AddRange(resultadoSaida.Erros!);
            }

            // Entrada por produção: o produto acabado.
            var entrada = new RegistrarMovimentacaoDeEstoqueEntrada(
                IdProduto: evento.IdProdutoFabricado.Valor,
                Tipo: TipoDeMovimentacao.EntradaPorProducao,
                Quantidade: evento.QuantidadeProduzida,
                OrigemTipo: TipoOrigemMovimentacao.Producao,
                OrigemIdReferencia: evento.IdOrdemDeProducao.Valor,
                PermitirSaldoNegativo: false);

            var resultadoEntrada = await _registrarMovimentacao.ExecutarAsync(entrada, cancellationToken);

            if (resultadoEntrada.EhFalha)
                erros.AddRange(resultadoEntrada.Erros!);

            if (erros.Count > 0)
                return Resultado<bool>.Falha(erros);

            return Resultado<bool>.Sucesso(true);
        }
    }
}
