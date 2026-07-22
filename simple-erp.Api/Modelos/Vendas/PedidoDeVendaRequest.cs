namespace simple_erp.Api.Modelos.Vendas
{
    /// <summary>
    /// Item enviado no corpo do POST de criação. O desconto aqui é do item; o do
    /// pedido é separado. O subtotal não entra: é o domínio que calcula, e aceitá-lo
    /// do cliente permitiria um total inconsistente com os itens.
    /// </summary>
    public sealed record ItemDePedidoDeVendaRequest(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto = 0m);

    /// <summary>
    /// Corpo do POST de criação. O número do pedido não vem do cliente — é sequencial
    /// e atribuído pelo domínio.
    /// </summary>
    public sealed record CriarPedidoDeVendaRequest(
        long IdCliente,
        IReadOnlyCollection<ItemDePedidoDeVendaRequest>? Itens,
        decimal DescontoDoPedido = 0m);

    /// <summary>
    /// Corpo do POST que acrescenta um item a um pedido existente. O pedido vem da
    /// rota, então só os dados do item entram aqui.
    /// </summary>
    public sealed record AdicionarItemAoPedidoDeVendaRequest(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto = 0m);

    /// <summary>
    /// Corpo do PUT de desconto do pedido. Substitui o valor anterior — o domínio faz
    /// <c>DescontoDoPedido = desconto</c>, não soma.
    /// </summary>
    public sealed record AplicarDescontoNoPedidoDeVendaRequest(decimal Desconto);

    /// <summary>
    /// Corpo do POST de cancelamento. O motivo é obrigatório no domínio e viaja no
    /// evento <c>PedidoDeVendaCancelado</c>, então não é um campo decorativo.
    /// </summary>
    public sealed record CancelarPedidoDeVendaRequest(string Motivo);
}
