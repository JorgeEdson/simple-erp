using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor
{
    /// <summary>
    /// Item de uma composição (receita/BOM): um insumo (produto matéria-prima) e a
    /// quantidade necessária para produzir 1 unidade do produto fabricado.
    /// </summary>
    public sealed class ItemDeComposicao
        : ObjetoDeValor<PropriedadesItemDeComposicao, IConfiguracaoObjetoDeValor>
    {
        private ItemDeComposicao(
            PropriedadesItemDeComposicao valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<ItemDeComposicao> TentarCriar(
            Id idInsumo,
            Quantidade quantidadePorUnidade,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var erros = new List<string>();

            if (idInsumo is null)
                erros.Add("INSUMO_OBRIGATORIO");

            if (quantidadePorUnidade is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (erros.Count > 0)
                return Resultado<ItemDeComposicao>.Falha(erros);

            var propriedades = new PropriedadesItemDeComposicao(
                IdInsumo: idInsumo!.Valor,
                QuantidadePorUnidade: quantidadePorUnidade!.Valor);

            return Resultado<ItemDeComposicao>.Sucesso(
                new ItemDeComposicao(propriedades, configuracao));
        }

        public long IdInsumo => Valor.IdInsumo;
        public decimal QuantidadePorUnidade => Valor.QuantidadePorUnidade;

        public bool RefereInsumo(long idInsumo) => Valor.IdInsumo == idInsumo;

        public override string ToString()
        {
            return $"Insumo[{IdInsumo}] x {QuantidadePorUnidade:0.####}/un";
        }
    }

    public record PropriedadesItemDeComposicao(
        long IdInsumo,
        decimal QuantidadePorUnidade);
}
