namespace simple_erp.Api.Modelos.Producao
{
    /// <summary>
    /// Corpo do POST de ordem de produção. Só o que o usuário decide: o que produzir e
    /// quanto. As necessidades de insumo são derivadas da composição ativa pelo
    /// domínio, então aceitá-las aqui permitiria uma ordem divergente da receita.
    /// </summary>
    public sealed record CriarOrdemDeProducaoRequest(
        long IdProdutoFabricado,
        decimal QuantidadeAProduzir);

    /// <summary>
    /// Item da receita: quanto do insumo entra em <em>uma</em> unidade do produto
    /// fabricado. A quantidade é por unidade, não total — é a ordem de produção que
    /// multiplica pela quantidade a produzir.
    /// </summary>
    public sealed record ItemDeComposicaoRequest(
        long IdInsumo,
        decimal QuantidadePorUnidade);

    /// <summary>
    /// Corpo do POST que define uma composição. O produto fabricado vem da rota, então
    /// só os itens entram aqui. Cada POST cria uma versão nova — a versão anterior é
    /// preservada, porque ordens antigas precisam continuar explicáveis pela receita
    /// que valia quando foram criadas.
    /// </summary>
    public sealed record DefinirComposicaoDeProdutoRequest(
        IReadOnlyCollection<ItemDeComposicaoRequest>? Itens);
}
