namespace simple_erp.Core.Compartilhado.Contratos.Observabilidade
{   
    public sealed record RegistroDeLog(
        string Mensagem,
        IReadOnlyDictionary<string, object?>? Propriedades = null,
        Exception? Exception = null);
}
