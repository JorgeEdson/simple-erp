using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor
{
    public sealed class Dinheiro : ObjetoDeValor<decimal, IConfiguracaoObjetoDeValor>
    {
        private const string ValorInvalido = "VALOR_MONETARIO_INVALIDO";
        private const string ValorNegativo = "VALOR_MONETARIO_NEGATIVO";

        private const int CasasDecimais = 2;

        private Dinheiro(decimal valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<Dinheiro> TentarCriar(
            decimal valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                if (valor < 0m)
                    return Resultado<Dinheiro>.Falha(ValorNegativo);

                var valorAjustado = Math.Round(valor, CasasDecimais, MidpointRounding.AwayFromZero);

                return Resultado<Dinheiro>.Sucesso(new Dinheiro(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Dinheiro>.Falha(ex.Message ?? ValorInvalido);
            }
        }

        public static Dinheiro Zero => new(0m);

        public string Formatado => Valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));

        public override string ToString()
        {
            return Valor.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
