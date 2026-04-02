using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor
{
    public sealed class Cnpj : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string CnpjInvalido = "CNPJ_INVALIDO";
        private const int TamanhoCnpj = 14;

        private Cnpj(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        

        public static Resultado<Cnpj> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var digitos = SomenteDigitos(valor);

                if (digitos.Length != TamanhoCnpj)
                    return Resultado<Cnpj>.Falha(CnpjInvalido);

                if (!Regex.IsMatch(digitos, @"^\d{14}$"))
                    return Resultado<Cnpj>.Falha(CnpjInvalido);

                if (TodosOsDigitosSaoIguais(digitos))
                    return Resultado<Cnpj>.Falha(CnpjInvalido);

                return Resultado<Cnpj>.Sucesso(new Cnpj(digitos, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Cnpj>.Falha(ex.Message ?? CnpjInvalido);
            }
        }

        public string Formatado =>
            $"{Valor[..2]}.{Valor.Substring(2, 3)}.{Valor.Substring(5, 3)}/{Valor.Substring(8, 4)}-{Valor.Substring(12, 2)}";

        public static string Formatar(string valor)
        {
            var digitos = SomenteDigitos(valor);

            if (digitos.Length != TamanhoCnpj)
                return valor;

            return $"{digitos[..2]}.{digitos.Substring(2, 3)}.{digitos.Substring(5, 3)}/{digitos.Substring(8, 4)}-{digitos.Substring(12, 2)}";
        }

        private static string SomenteDigitos(string? valor)
        {
            return Regex.Replace(valor ?? string.Empty, @"\D", "");
        }

        private static bool TodosOsDigitosSaoIguais(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            var primeiro = valor[0];
            return valor.All(c => c == primeiro);
        }
    }
}
