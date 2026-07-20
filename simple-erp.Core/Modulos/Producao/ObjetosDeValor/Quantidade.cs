using simple_erp.Core.Compartilhado.Base;
using System;

namespace simple_erp.Core.Modulos.Producao.ObjetosDeValor
{
    public sealed class Quantidade : ObjetoDeValor<decimal, IConfiguracaoObjetoDeValor>
    {
        private const string QuantidadeInvalida = "QUANTIDADE_INVALIDA";
        private const string QuantidadeDeveSerPositiva = "QUANTIDADE_DEVE_SER_POSITIVA";

        private Quantidade(decimal valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<Quantidade> TentarCriar(
            decimal valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                if (valor <= 0m)
                    return Resultado<Quantidade>.Falha(QuantidadeDeveSerPositiva);

                return Resultado<Quantidade>.Sucesso(new Quantidade(valor, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Quantidade>.Falha(ex.Message ?? QuantidadeInvalida);
            }
        }

        public override string ToString()
        {
            return Valor.ToString("0.####");
        }
    }
}
