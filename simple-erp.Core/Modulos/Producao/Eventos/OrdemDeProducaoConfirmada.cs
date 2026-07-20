using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    public sealed class OrdemDeProducaoConfirmada : EventoDeDominio
    {
        public OrdemDeProducaoConfirmada(Id idOrdemDeProducao, Id idProdutoFabricado)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
            IdProdutoFabricado = idProdutoFabricado;
        }

        public Id IdOrdemDeProducao { get; }
        public Id IdProdutoFabricado { get; }
    }
}
