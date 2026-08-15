using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    public sealed class OrdemDeProducaoConfirmada : EventoDeDominio
    {
        public OrdemDeProducaoConfirmada(Guid idOrdemDeProducao, Guid idProdutoFabricado)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
            IdProdutoFabricado = idProdutoFabricado;
        }

        public Guid IdOrdemDeProducao { get; }
        public Guid IdProdutoFabricado { get; }
    }
}
