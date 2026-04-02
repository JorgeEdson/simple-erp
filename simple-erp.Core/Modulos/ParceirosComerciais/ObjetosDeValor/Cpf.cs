using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor
{
    public sealed class Cpf : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string CpfInvalido = "CPF_INVALIDO";

        private Cpf(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public string Formatado =>
            $"{Valor[..3]}.{Valor.Substring(3, 3)}.{Valor.Substring(6, 3)}-{Valor.Substring(9, 2)}";

        public string DigitosVerificadores => Valor.Substring(9, 2);

        public string DigitosBase => Valor.Substring(0, 9);

        public bool PossuiMesmaRaiz(Cpf cpf)
        {
            return DigitosBase == cpf.DigitosBase;
        }

        

        public static Resultado<Cpf> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var digitos = ExtrairDigitos(valor);

                if (digitos.Length != 11)
                    return Resultado<Cpf>.Falha(CpfInvalido);

                if (TodosOsDigitosSaoIguais(digitos))
                    return Resultado<Cpf>.Falha(CpfInvalido);

                if (!PossuiDigitosVerificadoresValidos(digitos))
                    return Resultado<Cpf>.Falha(CpfInvalido);

                return Resultado<Cpf>.Sucesso(new Cpf(digitos, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Cpf>.Falha(ex.Message ?? CpfInvalido);
            }
        }

        private static string ExtrairDigitos(string? valor)
        {
            return Regex.Replace(valor ?? string.Empty, @"\D", "");
        }

        private static bool PossuiDigitosVerificadoresValidos(string digitos)
        {
            var primeiroDigito = CalcularDigitoVerificador(digitos[..9], 10);
            var segundoDigito = CalcularDigitoVerificador($"{digitos[..9]}{primeiroDigito}", 11);

            return digitos.EndsWith($"{primeiroDigito}{segundoDigito}");
        }

        private static int CalcularDigitoVerificador(string cpfBase, int pesoInicial)
        {
            var total = cpfBase
                .Select((caractere, indice) => (caractere - '0') * (pesoInicial - indice))
                .Sum();

            var resto = total % 11;
            return resto < 2 ? 0 : 11 - resto;
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
