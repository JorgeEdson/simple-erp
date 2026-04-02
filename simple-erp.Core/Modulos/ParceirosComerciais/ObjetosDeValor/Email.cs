using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor
{
    public sealed class Email : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string EmailInvalido = "EMAIL_INVALIDO";

        private Email(string valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public string Local => Valor.Split('@')?.Length > 0
            ? Valor.Split('@')[0]
            : string.Empty;

        public string Dominio => Valor.Split('@')?.Length > 1
            ? Valor.Split('@')[1]
            : string.Empty;

        

        public static Resultado<Email> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var email = (valor ?? string.Empty).Trim().ToLowerInvariant();

                var regex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");

                if (!regex.IsMatch(email))
                    return Resultado<Email>.Falha(EmailInvalido);

                return Resultado<Email>.Sucesso(new Email(email, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Email>.Falha(ex.Message ?? EmailInvalido);
            }
        }
    }
}
