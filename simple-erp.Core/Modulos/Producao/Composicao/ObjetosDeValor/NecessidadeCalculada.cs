namespace simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor
{
    /// <summary>
    /// Resultado neutro do cálculo de necessidade de matéria-prima feito pela receita
    /// (ComposicaoDeProduto) para uma dada quantidade a produzir. É consumido pelo
    /// núcleo de Produção para montar as necessidades da ordem, sem acoplar a receita
    /// ao agregado OrdemDeProducao.
    /// </summary>
    public sealed record NecessidadeCalculada(
        long IdInsumo,
        decimal QuantidadeTotal);
}
