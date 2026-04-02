using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace simple_erp.Core.Compartilhado.ObjetosDeValor
{
    public sealed class DataValor : ObjetoDeValor<DateTime, IConfiguracaoObjetoDeValor>
    {
        private const string DataInvalida = "DATA_INVALIDA";

        private DataValor(DateTime valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        

        public static Resultado<DataValor> TentarCriar(
            object valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var data = ConverterDeDesconhecido(valor);

                if (data is null)
                    return Resultado<DataValor>.Falha(DataInvalida);

                return Resultado<DataValor>.Sucesso(new DataValor(data.Value, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<DataValor>.Falha(ex.Message ?? DataInvalida);
            }
        }

        public static DateTime? ConverterDeDesconhecido(object? valor)
        {
            if (valor is null)
                return null;

            if (valor is DateTime data)
                return data;

            if (valor is DateTimeOffset dataOffset)
                return dataOffset.UtcDateTime;

            if (valor is string texto)
            {
                if (DateTime.TryParse(
                        texto,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                        out var dataTexto))
                {
                    return dataTexto;
                }

                if (DateTime.TryParse(texto, out var dataLocal))
                    return dataLocal;

                return null;
            }

            if (valor is long timestampLong)
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(timestampLong).UtcDateTime;
                }
                catch
                {
                    return null;
                }
            }

            if (valor is int timestampInt)
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(timestampInt).UtcDateTime;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public static string Formatar(DateTime? data)
        {
            if (data is null)
                return "—";

            var d = data.Value;
            return $"{d:dd/MM/yyyy}";
        }
    }
}
