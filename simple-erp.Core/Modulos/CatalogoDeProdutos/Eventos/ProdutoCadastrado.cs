using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.Eventos
{
    public sealed class ProdutoCadastrado : EventoDeDominio
    {
        public ProdutoCadastrado(
            Id idProduto,
            CodigoProduto codigo,
            DescricaoProduto descricao)
            : base(idProduto)
        {
            IdProduto = idProduto;
            Codigo = codigo;
            Descricao = descricao;
        }

        public Id IdProduto { get; }
        public CodigoProduto Codigo { get; }
        public DescricaoProduto Descricao { get; }
    }
}
