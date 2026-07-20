using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    /// <summary>Fato: uma baixa (parcial ou total) foi aplicada ao título.</summary>
    public sealed class TituloBaixado : EventoDeDominio
    {
        public TituloBaixado(
            Id idTitulo,
            decimal valorBaixa,
            decimal valorBaixadoAcumulado,
            decimal saldoDevedor)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
            ValorBaixa = valorBaixa;
            ValorBaixadoAcumulado = valorBaixadoAcumulado;
            SaldoDevedor = saldoDevedor;
        }

        public Id IdTitulo { get; }
        public decimal ValorBaixa { get; }
        public decimal ValorBaixadoAcumulado { get; }
        public decimal SaldoDevedor { get; }
    }
}
