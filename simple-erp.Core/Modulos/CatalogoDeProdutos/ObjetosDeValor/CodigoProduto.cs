using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor
{
    public sealed class CodigoProduto : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string CodigoMuitoCurto = "CODIGO_PRODUTO_MUITO_CURTO";
        private const string CodigoMuitoLongo = "CODIGO_PRODUTO_MUITO_LONGO";
        private const string CodigoFormatoInvalido = "CODIGO_PRODUTO_FORMATO_INVALIDO";

        private const int TamanhoMinimo = 1;
        private const int TamanhoMaximo = 50;

        private CodigoProduto(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<CodigoProduto> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim().ToUpperInvariant();

                if (valorAjustado.Length < TamanhoMinimo)
                    return Resultado<CodigoProduto>.Falha(CodigoMuitoCurto);

                if (valorAjustado.Length > TamanhoMaximo)
                    return Resultado<CodigoProduto>.Falha(CodigoMuitoLongo);

                if (!Regex.IsMatch(valorAjustado, @"^[A-Z0-9\-_\.]+$"))
                    return Resultado<CodigoProduto>.Falha(CodigoFormatoInvalido);

                return Resultado<CodigoProduto>.Sucesso(new CodigoProduto(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<CodigoProduto>.Falha(ex.Message ?? CodigoFormatoInvalido);
            }
        }

        public override string ToString()
        {
            return Valor;
        }
    }
}
