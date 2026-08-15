using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    public sealed class OrdemDeProducaoCriada : EventoDeDominio
    {
        public OrdemDeProducaoCriada(
            Guid idOrdemDeProducao,
            Guid idProdutoFabricado,
            Guid idComposicao,
            decimal quantidadeAProduzir)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
            IdProdutoFabricado = idProdutoFabricado;
            IdComposicao = idComposicao;
            QuantidadeAProduzir = quantidadeAProduzir;
        }

        public Guid IdOrdemDeProducao { get; }
        public Guid IdProdutoFabricado { get; }
        public Guid IdComposicao { get; }
        public decimal QuantidadeAProduzir { get; }
    }
}
