using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects do módulo Vendas.
    ///
    /// Os itens do pedido são uma coleção de VOs sem identidade própria — pertencem
    /// integralmente ao agregado PedidoDeVenda —, então persistem como um array jsonb
    /// na própria linha do pedido, seguindo o mesmo padrão de Suprimentos.
    /// </summary>
    public static class ConversoresDeVendas
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        public static readonly ValueConverter<List<ItemDePedidoDeVenda>, string> ItensParaJson =
            new(
                itens => SerializarItens(itens),
                json => DesserializarItens(json));

        public static readonly ValueComparer<List<ItemDePedidoDeVenda>> ComparadorDeItens =
            new(
                (a, b) => SerializarItens(a) == SerializarItens(b),
                itens => SerializarItens(itens).GetHashCode(),
                itens => DesserializarItens(SerializarItens(itens)));

        private static string SerializarItens(List<ItemDePedidoDeVenda>? itens) =>
            JsonSerializer.Serialize(
                (itens ?? new List<ItemDePedidoDeVenda>()).Select(item => item.Valor).ToList(),
                OpcoesJson);

        private static List<ItemDePedidoDeVenda> DesserializarItens(string json)
        {
            var propriedades =
                JsonSerializer.Deserialize<List<PropriedadesItemDePedidoDeVenda>>(json, OpcoesJson)
                ?? new List<PropriedadesItemDePedidoDeVenda>();

            return propriedades
                .Select(p => ItemDePedidoDeVenda.TentarCriar(
                    Id.TentarCriar(p.IdProduto).Instancia!,
                    Quantidade.TentarCriar(p.Quantidade).Instancia!,
                    Dinheiro.TentarCriar(p.PrecoUnitario).Instancia!,
                    Dinheiro.TentarCriar(p.Desconto).Instancia!,
                    null).Instancia!)
                .ToList();
        }
    }
}
