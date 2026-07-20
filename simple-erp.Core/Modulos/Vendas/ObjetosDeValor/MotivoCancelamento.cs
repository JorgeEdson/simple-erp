using simple_erp.Core.Compartilhado.Base;
using System;

namespace simple_erp.Core.Modulos.Vendas.ObjetosDeValor
{
    public sealed class MotivoCancelamento : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string MotivoObrigatorio = "MOTIVO_CANCELAMENTO_OBRIGATORIO";
        private const string MotivoMuitoLongo = "MOTIVO_CANCELAMENTO_MUITO_LONGO";

        private const int TamanhoMinimo = 3;
        private const int TamanhoMaximo = 300;

        private MotivoCancelamento(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<MotivoCancelamento> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim();

                if (valorAjustado.Length < TamanhoMinimo)
                    return Resultado<MotivoCancelamento>.Falha(MotivoObrigatorio);

                if (valorAjustado.Length > TamanhoMaximo)
                    return Resultado<MotivoCancelamento>.Falha(MotivoMuitoLongo);

                return Resultado<MotivoCancelamento>.Sucesso(new MotivoCancelamento(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<MotivoCancelamento>.Falha(ex.Message ?? MotivoObrigatorio);
            }
        }

        public override string ToString() => Valor;
    }
}
