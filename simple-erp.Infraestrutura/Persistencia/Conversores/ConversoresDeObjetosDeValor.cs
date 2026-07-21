using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{   
    public static class ConversoresDeObjetosDeValor
    {
       
        public static readonly ValueConverter<Id, long> IdParaLong =
            new(
                id => id.Valor,
                valor => Id.TentarCriar(valor).Instancia!);

      
        public static readonly ValueComparer<Id> ComparadorDeId =
            new(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.Valor == b.Valor),
                id => id.Valor.GetHashCode(),
                id => Id.TentarCriar(id.Valor).Instancia!);

        public static readonly ValueConverter<Nome, string> NomeParaString =
            new(
                nome => nome.Valor,
                valor => Nome.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<Descricao, string> DescricaoParaString =
            new(
                descricao => descricao.Valor,
                valor => Descricao.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<DataValor, DateTime> DataValorParaDateTime =
            new(
                data => data.Valor,
                valor => DataValor.TentarCriar(valor, null).Instancia!);
    }
}
