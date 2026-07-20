using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    /// <summary>Fato: o título foi totalmente liquidado (saldo devedor zerado).</summary>
    public sealed class TituloLiquidado : EventoDeDominio
    {
        public TituloLiquidado(Id idTitulo)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
        }

        public Id IdTitulo { get; }
    }
}
