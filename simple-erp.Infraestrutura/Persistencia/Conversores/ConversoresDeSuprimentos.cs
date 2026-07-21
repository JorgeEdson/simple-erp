using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects do módulo Suprimentos.
    ///
    /// Os itens do pedido são uma coleção de VOs sem identidade própria — pertencem
    /// integralmente ao agregado PedidoDeCompra —, então persistem como um array jsonb
    /// na própria linha do pedido, seguindo o mesmo padrão do histórico de baixas do
    /// Financeiro. Carregar o pedido já traz seus itens, como uma unidade.
    /// </summary>
    public static class ConversoresDeSuprimentos
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        public static readonly ValueConverter<List<ItemDePedidoDeCompra>, string> ItensParaJson =
            new(
                itens => SerializarItens(itens),
                json => DesserializarItens(json));

        /// <summary>
        /// Comparador por valor da coleção de itens: o change tracker precisa detectar
        /// inclusões/remoções comparando conteúdo, e clonar em snapshot.
        /// </summary>
        public static readonly ValueComparer<List<ItemDePedidoDeCompra>> ComparadorDeItens =
            new(
                (a, b) => SerializarItens(a) == SerializarItens(b),
                itens => SerializarItens(itens).GetHashCode(),
                itens => DesserializarItens(SerializarItens(itens)));

        private static string SerializarItens(List<ItemDePedidoDeCompra>? itens)
        {
            var propriedades = (itens ?? new List<ItemDePedidoDeCompra>())
                .Select(item => item.Valor)
                .ToList();

            return JsonSerializer.Serialize(propriedades, OpcoesJson);
        }

        private static List<ItemDePedidoDeCompra> DesserializarItens(string json)
        {
            var propriedades =
                JsonSerializer.Deserialize<List<PropriedadesItemDePedidoDeCompra>>(json, OpcoesJson)
                ?? new List<PropriedadesItemDePedidoDeCompra>();

            return propriedades
                .Select(p => ItemDePedidoDeCompra.TentarCriar(
                    Id.TentarCriar(p.IdProduto).Instancia!,
                    Quantidade.TentarCriar(p.Quantidade).Instancia!,
                    Dinheiro.TentarCriar(p.CustoUnitario).Instancia!,
                    null).Instancia!)
                .ToList();
        }
    }
}
