using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{  
    public static class ConversoresDeParceirosComerciais
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        public static readonly ValueConverter<Documento, string> DocumentoParaString =
            new(
                documento => documento.Valor,
                valor => Documento.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<Email, string> EmailParaString =
            new(
                email => email.Valor,
                valor => Email.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<Endereco, string> EnderecoParaJson =
            new(
                endereco => JsonSerializer.Serialize(endereco.Valor, OpcoesJson),
                json => Endereco.TentarCriar(
                    JsonSerializer.Deserialize<PropriedadesEndereco>(json, OpcoesJson)!,
                    null).Instancia!);
    }
}
