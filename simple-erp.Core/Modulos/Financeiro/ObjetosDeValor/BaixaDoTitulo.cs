using simple_erp.Core.Compartilhado.Base;
using System;

namespace simple_erp.Core.Modulos.Financeiro.ObjetosDeValor
{   
    public sealed class BaixaDoTitulo
        : ObjetoDeValor<PropriedadesBaixaDoTitulo, IConfiguracaoObjetoDeValor>
    {
        private BaixaDoTitulo(
            PropriedadesBaixaDoTitulo valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<BaixaDoTitulo> TentarCriar(
            Dinheiro montante,
            DateTime? dataUtc = null,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            if (montante is null)
                return Resultado<BaixaDoTitulo>.Falha("VALOR_BAIXA_OBRIGATORIO");

            if (montante.Valor <= 0m)
                return Resultado<BaixaDoTitulo>.Falha("VALOR_BAIXA_INVALIDO");

            var propriedades = new PropriedadesBaixaDoTitulo(
                Montante: montante.Valor,
                DataUtc: dataUtc ?? DateTime.UtcNow);

            return Resultado<BaixaDoTitulo>.Sucesso(new BaixaDoTitulo(propriedades, configuracao));
        }

        public decimal Montante => Valor.Montante;
        public DateTime DataUtc => Valor.DataUtc;

        public override string ToString() => $"{Montante:0.00} em {DataUtc:dd/MM/yyyy}";
    }

    public record PropriedadesBaixaDoTitulo(
        decimal Montante,
        DateTime DataUtc);
}
