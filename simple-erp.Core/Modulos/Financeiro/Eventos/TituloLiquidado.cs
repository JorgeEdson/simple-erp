using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    /// <summary>Fato: o título foi totalmente liquidado (saldo devedor zerado).</summary>
    public sealed class TituloLiquidado : EventoDeDominio
    {
        public TituloLiquidado(Guid idTitulo)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
        }

        public Guid IdTitulo { get; }
    }
}
