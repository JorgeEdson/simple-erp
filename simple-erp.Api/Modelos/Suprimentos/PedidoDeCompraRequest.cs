namespace simple_erp.Api.Modelos.Suprimentos
{
    /// <summary>
    /// Item enviado no corpo do POST de criação. O subtotal não entra: é calculado
    /// pelo domínio a partir de quantidade × custo unitário, e aceitá-lo do cliente
    /// permitiria um pedido com total inconsistente com seus itens.
    /// </summary>
    public sealed record ItemDePedidoDeCompraRequest(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);

    /// <summary>
    /// Corpo do POST de criação. O pedido nasce em edição; o status não vem do
    /// cliente porque é o agregado que governa as transições.
    /// </summary>
    public sealed record CriarPedidoDeCompraRequest(
        long IdFornecedor,
        IReadOnlyCollection<ItemDePedidoDeCompraRequest>? Itens);

    /// <summary>
    /// Corpo do POST que acrescenta um item a um pedido existente. O pedido vem da
    /// rota, então só os dados do item entram aqui.
    /// </summary>
    public sealed record AdicionarItemAoPedidoDeCompraRequest(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);
}
