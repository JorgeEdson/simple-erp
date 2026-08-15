using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    /// <summary>
    /// Conversores dos Value Objects compartilhados. O identificador não aparece aqui:
    /// desde que passou a ser <see cref="Guid"/>, o EF Core o mapeia nativamente para
    /// <c>uuid</c> sem conversor nem comparador.
    /// </summary>
    public static class ConversoresDeObjetosDeValor
    {
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
