using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor
{
    public sealed class UnidadeDeMedida : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string UnidadeMedidaInvalida = "UNIDADE_MEDIDA_INVALIDA";

        public static readonly IReadOnlyCollection<string> UnidadesPermitidas = new[]
        {
            "UN",  // Unidade
            "PC",  // Peça
            "KG",  // Quilograma
            "G",   // Grama
            "MG",  // Miligrama
            "T",   // Tonelada
            "L",   // Litro
            "ML",  // Mililitro
            "M",   // Metro
            "CM",  // Centímetro
            "MM",  // Milímetro
            "M2",  // Metro quadrado
            "M3",  // Metro cúbico
            "CX",  // Caixa
            "PCT", // Pacote
            "DZ",  // Dúzia
            "PAR", // Par
            "ROL", // Rolo
            "FRD", // Fardo
            "SC"   // Saco
        };

        private UnidadeDeMedida(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<UnidadeDeMedida> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorAjustado = (valor ?? string.Empty).Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(valorAjustado))
                    return Resultado<UnidadeDeMedida>.Falha(UnidadeMedidaInvalida);

                if (!UnidadesPermitidas.Contains(valorAjustado))
                    return Resultado<UnidadeDeMedida>.Falha(UnidadeMedidaInvalida);

                return Resultado<UnidadeDeMedida>.Sucesso(
                    new UnidadeDeMedida(valorAjustado, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<UnidadeDeMedida>.Falha(ex.Message ?? UnidadeMedidaInvalida);
            }
        }

        public override string ToString()
        {
            return Valor;
        }
    }
}
