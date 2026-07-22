namespace simple_erp.Api.Comum
{
    /// <summary>
    /// Corpo padronizado de erro da API. Os erros do domínio são códigos estáveis
    /// (ex.: "CLIENTE_NAO_ENCONTRADO", "DOCUMENTO_INVALIDO") — devolvê-los como uma
    /// lista permite ao cliente da API tratar cada caso, e não depender de mensagens.
    /// </summary>
    public sealed record RespostaDeErro(IReadOnlyCollection<string> Erros);
}
