using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Financeiro.Eventos
{
    public sealed class TituloEmitido : EventoDeDominio
    {
        public TituloEmitido(
            Guid idTitulo,
            TipoDeTitulo tipo,
            Guid idParceiro,
            decimal valorOriginal)
            : base(idTitulo)
        {
            IdTitulo = idTitulo;
            Tipo = tipo;
            IdParceiro = idParceiro;
            ValorOriginal = valorOriginal;
        }

        public Guid IdTitulo { get; }
        public TipoDeTitulo Tipo { get; }
        public Guid IdParceiro { get; }
        public decimal ValorOriginal { get; }
    }
}
