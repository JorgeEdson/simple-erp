using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Estoque.Eventos
{
    public sealed class SaldoDeEstoqueCriado : EventoDeDominio
    {
        public SaldoDeEstoqueCriado(Guid idSaldoDeEstoque, Guid idProduto)
            : base(idSaldoDeEstoque)
        {
            IdSaldoDeEstoque = idSaldoDeEstoque;
            IdProduto = idProduto;
        }

        public Guid IdSaldoDeEstoque { get; }
        public Guid IdProduto { get; }
    }
}
