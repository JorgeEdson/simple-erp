using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Composicao.Eventos
{
    public sealed class ComposicaoDeProdutoInativada : EventoDeDominio
    {
        public ComposicaoDeProdutoInativada(Guid idComposicao, Guid idProdutoFabricado, int versao)
            : base(idComposicao)
        {
            IdComposicao = idComposicao;
            IdProdutoFabricado = idProdutoFabricado;
            Versao = versao;
        }

        public Guid IdComposicao { get; }
        public Guid IdProdutoFabricado { get; }
        public int Versao { get; }
    }
}
