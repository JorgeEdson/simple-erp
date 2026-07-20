using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Producao.ObjetosDeValor
{
    /// <summary>
    /// Necessidade de um insumo em uma ordem de produção: quantidade total de uma
    /// matéria-prima requerida para produzir a quantidade planejada, já calculada a
    /// partir da composição ativa.
    /// </summary>
    public sealed class NecessidadeDeMateriaPrima
        : ObjetoDeValor<PropriedadesNecessidadeDeMateriaPrima, IConfiguracaoObjetoDeValor>
    {
        private NecessidadeDeMateriaPrima(
            PropriedadesNecessidadeDeMateriaPrima valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<NecessidadeDeMateriaPrima> TentarCriar(
            Id idInsumo,
            Quantidade quantidadeNecessaria,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var erros = new List<string>();

            if (idInsumo is null)
                erros.Add("INSUMO_OBRIGATORIO");

            if (quantidadeNecessaria is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (erros.Count > 0)
                return Resultado<NecessidadeDeMateriaPrima>.Falha(erros);

            var propriedades = new PropriedadesNecessidadeDeMateriaPrima(
                IdInsumo: idInsumo!.Valor,
                QuantidadeNecessaria: quantidadeNecessaria!.Valor);

            return Resultado<NecessidadeDeMateriaPrima>.Sucesso(
                new NecessidadeDeMateriaPrima(propriedades, configuracao));
        }

        public long IdInsumo => Valor.IdInsumo;
        public decimal QuantidadeNecessaria => Valor.QuantidadeNecessaria;

        public override string ToString()
        {
            return $"Insumo[{IdInsumo}] necessita {QuantidadeNecessaria:0.####}";
        }
    }

    public record PropriedadesNecessidadeDeMateriaPrima(
        long IdInsumo,
        decimal QuantidadeNecessaria);
}
