using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor
{
    public sealed class DescricaoProduto : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string DescricaoMuitoCurta = "DESCRICAO_PRODUTO_MUITO_CURTA";
        private const string DescricaoMuitoLonga = "DESCRICAO_PRODUTO_MUITO_LONGA";

        private const int TamanhoMinimo = 3;
        private const int TamanhoMaximo = 200;

        private DescricaoProduto(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<DescricaoProduto> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim();

                if (valorAjustado.Length < TamanhoMinimo)
                    return Resultado<DescricaoProduto>.Falha(DescricaoMuitoCurta);

                if (valorAjustado.Length > TamanhoMaximo)
                    return Resultado<DescricaoProduto>.Falha(DescricaoMuitoLonga);

                return Resultado<DescricaoProduto>.Sucesso(
                    new DescricaoProduto(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<DescricaoProduto>.Falha(ex.Message);
            }
        }

        public override string ToString()
        {
            return Valor;
        }
    }
}
