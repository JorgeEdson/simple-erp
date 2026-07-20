using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Producao.Eventos
{
    /// <summary>
    /// Fato: a ordem de produção foi concluída. Carrega o produto acabado e sua
    /// quantidade (entrada por produção) e as necessidades consumidas (saída por
    /// produção), servindo de referência para as movimentações de estoque geradas.
    /// </summary>
    public sealed class OrdemDeProducaoConcluida : EventoDeDominio
    {
        public OrdemDeProducaoConcluida(
            Id idOrdemDeProducao,
            Id idProdutoFabricado,
            decimal quantidadeProduzida,
            IReadOnlyCollection<InsumoConsumido> insumosConsumidos)
            : base(idOrdemDeProducao)
        {
            IdOrdemDeProducao = idOrdemDeProducao;
            IdProdutoFabricado = idProdutoFabricado;
            QuantidadeProduzida = quantidadeProduzida;
            InsumosConsumidos = insumosConsumidos;
        }

        public Id IdOrdemDeProducao { get; }
        public Id IdProdutoFabricado { get; }
        public decimal QuantidadeProduzida { get; }
        public IReadOnlyCollection<InsumoConsumido> InsumosConsumidos { get; }
    }

    public sealed record InsumoConsumido(long IdInsumo, decimal Quantidade);
}
