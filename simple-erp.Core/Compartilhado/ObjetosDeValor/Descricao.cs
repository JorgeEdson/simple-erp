using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Compartilhado.ObjetosDeValor
{
    public sealed class Descricao : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string DescricaoMuitoCurta = "DESCRICAO_MUITO_CURTA";
        private const string DescricaoMuitoLonga = "DESCRICAO_MUITO_LONGA";

        private const int TamanhoMinimo = 20;
        private const int TamanhoMaximo = 2000;

        private Descricao(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }


        public static Resultado<Descricao> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim();

                if (valorAjustado.Length < TamanhoMinimo)
                    return Resultado<Descricao>.Falha(DescricaoMuitoCurta);

                if (valorAjustado.Length > TamanhoMaximo)
                    return Resultado<Descricao>.Falha(DescricaoMuitoLonga);

                return Resultado<Descricao>.Sucesso(new Descricao(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Descricao>.Falha(ex.Message);
            }
        }
    }
}
