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
            Guid idInsumo,
            Quantidade quantidadePorUnidade,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var erros = new List<string>();

            if (idInsumo == Guid.Empty)
                erros.Add("INSUMO_OBRIGATORIO");

            if (quantidadePorUnidade is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (erros.Count > 0)
                return Resultado<ItemDeComposicao>.Falha(erros);

            var propriedades = new PropriedadesItemDeComposicao(
                IdInsumo: idInsumo,
                QuantidadePorUnidade: quantidadePorUnidade!.Valor);

            return Resultado<ItemDeComposicao>.Sucesso(
                new ItemDeComposicao(propriedades, configuracao));
        }

        public Guid IdInsumo => Valor.IdInsumo;
        public decimal QuantidadePorUnidade => Valor.QuantidadePorUnidade;

        public bool RefereInsumo(Guid idInsumo) => Valor.IdInsumo == idInsumo;

        public override string ToString()
        {
            return $"Insumo[{IdInsumo}] x {QuantidadePorUnidade:0.####}/un";
        }
    }

    public record PropriedadesItemDeComposicao(
        Guid IdInsumo,
        decimal QuantidadePorUnidade);
}
