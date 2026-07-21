using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects do módulo Estoque.
    ///
    /// OrigemDaMovimentacao é o documento de referência que deu causa à movimentação
    /// (um pedido de compra/venda, uma ordem de produção, um ajuste). É um VO composto
    /// (tipo + id de referência) e persiste como jsonb, à imagem do Endereco em
    /// Parceiros: o Postgres permite filtrar por dentro do json
    /// (origem-&gt;&gt;'Tipo') no extrato paginado.
    ///
    /// Quantidade não ganha conversor: a entidade guarda os valores como decimal puro
    /// (o VO Quantidade é usado apenas na regra de negócio, na hora de movimentar).
    /// Os enums Tipo e Sentido são mapeados como int diretamente na configuração.
    /// </summary>
    public static class ConversoresDeEstoque
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        public static readonly ValueConverter<OrigemDaMovimentacao, string> OrigemParaJson =
            new(
                origem => JsonSerializer.Serialize(origem.Valor, OpcoesJson),
                json => OrigemDoJson(json));

        private static OrigemDaMovimentacao OrigemDoJson(string json)
        {
            var propriedades = JsonSerializer.Deserialize<PropriedadesOrigemDaMovimentacao>(json, OpcoesJson)!;
            return OrigemDaMovimentacao.TentarCriar(propriedades.Tipo, propriedades.IdReferencia, null).Instancia!;
        }
    }
}
