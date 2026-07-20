using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    public sealed class TituloEmitido : EventoDeDominio
    {
        public TituloEmitido(
            Id idTitulo,
            TipoDeTitulo tipo,
            Id idParceiro,
            decimal valorOriginal)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
            Tipo = tipo;
            IdParceiro = idParceiro;
            ValorOriginal = valorOriginal;
        }

        public Id IdTitulo { get; }
        public TipoDeTitulo Tipo { get; }
        public Id IdParceiro { get; }
        public decimal ValorOriginal { get; }
    }
}
