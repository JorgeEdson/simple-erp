using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.Eventos
{
    public sealed class ProdutoClassificadoComoMateriaPrima : EventoDeDominio
    {
        public ProdutoClassificadoComoMateriaPrima(Id idProduto)
            : base(idProduto)
        {
            IdProduto = idProduto;
        }

        public Id IdProduto { get; }
    }
}
