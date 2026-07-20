using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    public sealed class OrdemDeProducaoCriada : EventoDeDominio
    {
        public OrdemDeProducaoCriada(
            Id idOrdemDeProducao,
            Id idProdutoFabricado,
            Id idComposicao,
            decimal quantidadeAProduzir)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
            IdProdutoFabricado = idProdutoFabricado;
            IdComposicao = idComposicao;
            QuantidadeAProduzir = quantidadeAProduzir;
        }

        public Id IdOrdemDeProducao { get; }
        public Id IdProdutoFabricado { get; }
        public Id IdComposicao { get; }
        public decimal QuantidadeAProduzir { get; }
    }
}
