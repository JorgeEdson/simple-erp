using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;

namespace simple_erp.Infraestrutura.Persistencia.Conversores
{
    public static class ConversoresDeCatalogoDeProdutos
    {
        public static readonly ValueConverter<CodigoProduto, string> CodigoParaString =
            new(
                codigo => codigo.Valor,
                valor => CodigoProduto.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<DescricaoProduto, string> DescricaoParaString =
            new(
                descricao => descricao.Valor,
                valor => DescricaoProduto.TentarCriar(valor, null).Instancia!);

        public static readonly ValueConverter<UnidadeDeMedida, string> UnidadeDeMedidaParaString =
            new(
                unidade => unidade.Valor,
                valor => UnidadeDeMedida.TentarCriar(valor, null).Instancia!);
    }
}
