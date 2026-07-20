using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Estoque.Eventos
{
    public sealed class SaldoDeEstoqueCriado : EventoDeDominio
    {
        public SaldoDeEstoqueCriado(Id idSaldoDeEstoque, Id idProduto)
            : base(idSaldoDeEstoque)
        {
            IdSaldoDeEstoque = idSaldoDeEstoque;
            IdProduto = idProduto;
        }

        public Id IdSaldoDeEstoque { get; }
        public Id IdProduto { get; }
    }
}
