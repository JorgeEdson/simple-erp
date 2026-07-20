namespace simple_erp.Core.Modulos.Estoque.ObjetosDeValor
{
    /// <summary>
    /// Tipos de movimentação de estoque previstos nos requisitos funcionais (seção 3):
    /// entrada por compra, saída por venda, saída por produção (consumo de insumos),
    /// entrada por produção (produto acabado) e ajuste manual (inventário/correção),
    /// este último desdobrado em ajuste positivo e negativo para carregar o sentido.
    /// </summary>
    public enum TipoDeMovimentacao
    {
        EntradaPorCompra = 1,
        SaidaPorVenda = 2,
        SaidaPorProducao = 3,
        EntradaPorProducao = 4,
        AjustePositivo = 5,
        AjusteNegativo = 6
    }

    public static class TiposDeMovimentacao
    {
        public static SentidoDaMovimentacao Sentido(TipoDeMovimentacao tipo)
        {
            return tipo switch
            {
                TipoDeMovimentacao.EntradaPorCompra => SentidoDaMovimentacao.Entrada,
                TipoDeMovimentacao.EntradaPorProducao => SentidoDaMovimentacao.Entrada,
                TipoDeMovimentacao.AjustePositivo => SentidoDaMovimentacao.Entrada,
                TipoDeMovimentacao.SaidaPorVenda => SentidoDaMovimentacao.Saida,
                TipoDeMovimentacao.SaidaPorProducao => SentidoDaMovimentacao.Saida,
                TipoDeMovimentacao.AjusteNegativo => SentidoDaMovimentacao.Saida,
                _ => SentidoDaMovimentacao.Entrada
            };
        }

        public static bool EhEntrada(TipoDeMovimentacao tipo) =>
            Sentido(tipo) == SentidoDaMovimentacao.Entrada;

        public static bool EhSaida(TipoDeMovimentacao tipo) =>
            Sentido(tipo) == SentidoDaMovimentacao.Saida;
    }
}
