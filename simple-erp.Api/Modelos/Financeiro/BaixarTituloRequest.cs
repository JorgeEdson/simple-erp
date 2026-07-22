namespace simple_erp.Api.Modelos.Financeiro
{
    /// <summary>
    /// Corpo do POST de baixa. Só o montante: o título é identificado pela rota, e a
    /// data da baixa é do domínio (não do cliente), para o extrato não ficar sujeito
    /// ao relógio de quem chama a API.
    /// </summary>
    public sealed record BaixarTituloRequest(decimal Valor);
}
