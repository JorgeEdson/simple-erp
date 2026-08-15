using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Estoque.Eventos
{
    /// <summary>
    /// Fato: o saldo de um produto foi movimentado. Carrega o tipo, a quantidade,
    /// o sentido e o saldo resultante — informação suficiente para reações externas
    /// (ex.: alertas de estoque mínimo, projeções, auditoria).
    /// </summary>
    public sealed class SaldoDeEstoqueMovimentado : EventoDeDominio
    {
        public SaldoDeEstoqueMovimentado(
            Guid idSaldoDeEstoque,
            Guid idProduto,
            TipoDeMovimentacao tipo,
            SentidoDaMovimentacao sentido,
            decimal quantidade,
            decimal saldoResultante)
            : base(idSaldoDeEstoque)
        {
            IdSaldoDeEstoque = idSaldoDeEstoque;
            IdProduto = idProduto;
            Tipo = tipo;
            Sentido = sentido;
            Quantidade = quantidade;
            SaldoResultante = saldoResultante;
        }

        public Guid IdSaldoDeEstoque { get; }
        public Guid IdProduto { get; }
        public TipoDeMovimentacao Tipo { get; }
        public SentidoDaMovimentacao Sentido { get; }
        public decimal Quantidade { get; }
        public decimal SaldoResultante { get; }
    }
}
