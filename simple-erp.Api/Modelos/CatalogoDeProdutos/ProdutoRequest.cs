namespace simple_erp.Api.Modelos.CatalogoDeProdutos
{
    /// <summary>
    /// Corpo do POST de Produto. A classificação é opcional no cadastro: quando
    /// omitida, o agregado assume o padrão do domínio e o produto pode ser
    /// classificado depois pelos endpoints dedicados.
    /// </summary>
    public sealed record CadastrarProdutoRequest(
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string? Classificacao = null);

    /// <summary>
    /// Corpo do PUT de Produto. A classificação não entra aqui de propósito:
    /// mudar classificação é uma transição de estado do agregado, exposta nos
    /// endpoints de classificação, não uma edição de dados cadastrais.
    /// </summary>
    public sealed record EditarProdutoRequest(
        string Codigo,
        string Descricao,
        string UnidadeDeMedida);
}
