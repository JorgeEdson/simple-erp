using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Compartilhado.ObjetosDeValor
{
    public sealed class Nome : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string NomeMuitoCurto = "NOME_MUITO_CURTO";
        private const string NomeMuitoLongo = "NOME_MUITO_LONGO";

        private Nome(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        

        public static Resultado<Nome> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim();

                const int minimo = 3;
                const int maximo = 100;

                if (valorAjustado.Length < minimo)
                    return Resultado<Nome>.Falha(NomeMuitoCurto);

                if (valorAjustado.Length > maximo)
                    return Resultado<Nome>.Falha(NomeMuitoLongo);

                return Resultado<Nome>.Sucesso(new Nome(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Nome>.Falha(ex.Message);
            }
        }
    }
}
