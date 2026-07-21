using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects do módulo Produção e do subdomínio Composição.
    ///
    /// As necessidades de matéria-prima da ordem e os itens da receita são coleções de
    /// VOs sem identidade própria — pertencem integralmente aos seus agregados —, então
    /// persistem como arrays jsonb na própria linha, seguindo o mesmo padrão adotado em
    /// Suprimentos e Financeiro.
    /// </summary>
    public static class ConversoresDeProducao
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        // ----- Ordem de Produção: necessidades de matéria-prima -----

        public static readonly ValueConverter<List<NecessidadeDeMateriaPrima>, string> NecessidadesParaJson =
            new(
                necessidades => SerializarNecessidades(necessidades),
                json => DesserializarNecessidades(json));

        public static readonly ValueComparer<List<NecessidadeDeMateriaPrima>> ComparadorDeNecessidades =
            new(
                (a, b) => SerializarNecessidades(a) == SerializarNecessidades(b),
                necessidades => SerializarNecessidades(necessidades).GetHashCode(),
                necessidades => DesserializarNecessidades(SerializarNecessidades(necessidades)));

        // ----- Composição de Produto: itens da receita -----

        public static readonly ValueConverter<List<ItemDeComposicao>, string> ItensDeComposicaoParaJson =
            new(
                itens => SerializarItens(itens),
                json => DesserializarItens(json));

        public static readonly ValueComparer<List<ItemDeComposicao>> ComparadorDeItensDeComposicao =
            new(
                (a, b) => SerializarItens(a) == SerializarItens(b),
                itens => SerializarItens(itens).GetHashCode(),
                itens => DesserializarItens(SerializarItens(itens)));

        // ----- Serialização/desserialização -----

        private static string SerializarNecessidades(List<NecessidadeDeMateriaPrima>? necessidades) =>
            JsonSerializer.Serialize(
                (necessidades ?? new List<NecessidadeDeMateriaPrima>()).Select(n => n.Valor).ToList(),
                OpcoesJson);

        private static List<NecessidadeDeMateriaPrima> DesserializarNecessidades(string json)
        {
            var propriedades =
                JsonSerializer.Deserialize<List<PropriedadesNecessidadeDeMateriaPrima>>(json, OpcoesJson)
                ?? new List<PropriedadesNecessidadeDeMateriaPrima>();

            return propriedades
                .Select(p => NecessidadeDeMateriaPrima.TentarCriar(
                    Id.TentarCriar(p.IdInsumo).Instancia!,
                    Quantidade.TentarCriar(p.QuantidadeNecessaria).Instancia!,
                    null).Instancia!)
                .ToList();
        }

        private static string SerializarItens(List<ItemDeComposicao>? itens) =>
            JsonSerializer.Serialize(
                (itens ?? new List<ItemDeComposicao>()).Select(i => i.Valor).ToList(),
                OpcoesJson);

        private static List<ItemDeComposicao> DesserializarItens(string json)
        {
            var propriedades =
                JsonSerializer.Deserialize<List<PropriedadesItemDeComposicao>>(json, OpcoesJson)
                ?? new List<PropriedadesItemDeComposicao>();

            return propriedades
                .Select(p => ItemDeComposicao.TentarCriar(
                    Id.TentarCriar(p.IdInsumo).Instancia!,
                    Quantidade.TentarCriar(p.QuantidadePorUnidade).Instancia!,
                    null).Instancia!)
                .ToList();
        }
    }
}
