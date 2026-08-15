using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.Eventos
{
    public sealed class ProdutoReativado : EventoDeDominio
    {
        public ProdutoReativado(Guid idProduto)
            : base(idProduto)
        {
            IdProduto = idProduto;
        }

        public Guid IdProduto { get; }
    }
}
