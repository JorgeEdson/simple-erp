using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects do módulo Financeiro.
    ///
    /// OrigemDoTitulo (tipo + referência) e o histórico de baixas persistem como jsonb,
    /// à imagem do que se fez com Endereco/Origem. O histórico de baixas é uma coleção
    /// de VOs sem identidade própria — pertence integralmente ao agregado Titulo — então
    /// mora na mesma linha, como um array json, e não em tabela filha.
    /// </summary>
    public static class ConversoresDeFinanceiro
    {
        private static readonly JsonSerializerOptions OpcoesJson = JsonSerializerOptions.Default;

        public static readonly ValueConverter<OrigemDoTitulo, string> OrigemParaJson =
            new(
                origem => JsonSerializer.Serialize(origem.Valor, OpcoesJson),
                json => OrigemDoJson(json));

        public static readonly ValueConverter<List<BaixaDoTitulo>, string> BaixasParaJson =
            new(
                baixas => SerializarBaixas(baixas),
                json => DesserializarBaixas(json));

        /// <summary>
        /// Comparador por valor da coleção de baixas — necessário porque é um tipo de
        /// referência mutável: o change tracker precisa detectar inclusões comparando
        /// conteúdo (montante + data), e clonar em snapshot para não vazar a lista.
        /// </summary>
        public static readonly ValueComparer<List<BaixaDoTitulo>> ComparadorDeBaixas =
            new(
                (a, b) => SerializarBaixas(a) == SerializarBaixas(b),
                baixas => SerializarBaixas(baixas).GetHashCode(),
                baixas => DesserializarBaixas(SerializarBaixas(baixas)));

        private static OrigemDoTitulo OrigemDoJson(string json)
        {
            var propriedades = JsonSerializer.Deserialize<PropriedadesOrigemDoTitulo>(json, OpcoesJson)!;
            return OrigemDoTitulo.TentarCriar(propriedades.Tipo, propriedades.IdReferencia, null).Instancia!;
        }

        private static string SerializarBaixas(List<BaixaDoTitulo>? baixas)
        {
            var propriedades = (baixas ?? new List<BaixaDoTitulo>())
                .Select(b => b.Valor)
                .ToList();

            return JsonSerializer.Serialize(propriedades, OpcoesJson);
        }

        private static List<BaixaDoTitulo> DesserializarBaixas(string json)
        {
            var propriedades =
                JsonSerializer.Deserialize<List<PropriedadesBaixaDoTitulo>>(json, OpcoesJson)
                ?? new List<PropriedadesBaixaDoTitulo>();

            return propriedades
                .Select(p => BaixaDoTitulo.TentarCriar(
                    global::simple_erp.Core.Modulos.Financeiro.ObjetosDeValor.Dinheiro.TentarCriar(p.Montante).Instancia!,
                    p.DataUtc,
                    null).Instancia!)
                .ToList();
        }
    }
}
