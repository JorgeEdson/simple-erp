namespace simple_erp.Api.Modelos.ParceirosComerciais
{   
    public sealed record ParceiroRequest(
        string Documento,
        string Nome,
        string Email,
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string Pais);
}
