using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor
{
    public sealed class Endereco : ObjetoDeValor<PropriedadesEndereco, IConfiguracaoObjetoDeValor>
    {
        private const string EnderecoInvalido = "ENDERECO_INVALIDO";

        private Endereco(
            PropriedadesEndereco valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }              

        public static Resultado<Endereco> TentarCriar(
            PropriedadesEndereco valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var erros = new List<string>();

                var rua = NormalizarTexto(valor.Rua);
                var cidade = NormalizarTexto(valor.Cidade);
                var estado = NormalizarTexto(valor.Estado)?.ToUpperInvariant();
                var cep = NormalizarTexto(valor.Cep);
                var pais = NormalizarTexto(valor.Pais);

                var numero = NormalizarTexto(valor.Numero) ?? string.Empty;
                var complemento = NormalizarTexto(valor.Complemento) ?? string.Empty;
                var bairro = NormalizarTexto(valor.Bairro) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(rua) || rua.Length > 255)
                    erros.Add("RUA_INVALIDA");

                if (string.IsNullOrWhiteSpace(cidade) || cidade.Length > 100)
                    erros.Add("CIDADE_INVALIDA");

                if (string.IsNullOrWhiteSpace(estado) || estado.Length != 2)
                    erros.Add("ESTADO_INVALIDO");

                if (string.IsNullOrWhiteSpace(cep) || cep.Length < 5 || cep.Length > 20)
                    erros.Add("CEP_INVALIDO");

                if (string.IsNullOrWhiteSpace(pais) || pais.Length < 2 || pais.Length > 100)
                    erros.Add("PAIS_INVALIDO");

                if (erros.Count > 0)
                    return Resultado<Endereco>.Falha(erros);

                var propriedades = new PropriedadesEndereco(
                    Rua: rua!,
                    Numero: numero,
                    Complemento: complemento,
                    Bairro: bairro,
                    Cidade: cidade!,
                    Estado: estado!,
                    Cep: cep!,
                    Pais: pais!
                );

                return Resultado<Endereco>.Sucesso(new Endereco(propriedades, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Endereco>.Falha(ex.Message ?? EnderecoInvalido);
            }
        }

        public string Rua => Valor.Rua;
        public string Numero => Valor.Numero;
        public string Complemento => Valor.Complemento;
        public string Bairro => Valor.Bairro;
        public string Cidade => Valor.Cidade;
        public string Estado => Valor.Estado;
        public string Cep => Valor.Cep;
        public string Pais => Valor.Pais;

        public string RotuloCurto
        {
            get
            {
                var partes = new[] { Cidade, Estado }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                return partes.Length == 0 ? "—" : string.Join(" - ", partes);
            }
        }

        public static string FormatarCurto(string cidade, string estado)
        {
            var partes = new[] { cidade, estado }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            return partes.Length == 0 ? "—" : string.Join(" - ", partes);
        }

        private static string? NormalizarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }
    }

    public record PropriedadesEndereco(
    string Rua,
    string Numero,
    string Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep,
    string Pais
);
}
