using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor
{
    public sealed class Documento : ObjetoDeValor<string, IConfiguracaoObjetoDeValor>
    {
        private const string DocumentoInvalido = "DOCUMENTO_INVALIDO";

        private readonly Cpf? _cpf;
        private readonly Cnpj? _cnpj;

        private Documento(
            string valor,
            Cpf? cpf = null,
            Cnpj? cnpj = null,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
            _cpf = cpf;
            _cnpj = cnpj;
        }

        public bool EhCpf => _cpf is not null;
        public bool EhCnpj => _cnpj is not null;

        public Cpf? Cpf => _cpf;
        public Cnpj? Cnpj => _cnpj;

        public string Formatado
        {
            get
            {
                if (EhCpf)
                    return _cpf!.Formatado;

                if (EhCnpj)
                    return _cnpj!.Formatado;

                return Valor;
            }
        }

        

        public static Resultado<Documento> TentarCriar(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var resultadoCpf = Cpf.TentarCriar(valor, configuracao);
                if (resultadoCpf.EhSucesso)
                {
                    var cpf = resultadoCpf.Instancia;
                    return Resultado<Documento>.Sucesso(
                        new Documento(cpf.Valor, cpf: cpf, configuracao: configuracao));
                }

                var resultadoCnpj = Cnpj.TentarCriar(valor, configuracao);
                if (resultadoCnpj.EhSucesso)
                {
                    var cnpj = resultadoCnpj.Instancia;
                    return Resultado<Documento>.Sucesso(
                        new Documento(cnpj.Valor, cnpj: cnpj, configuracao: configuracao));
                }

                return Resultado<Documento>.Falha(DocumentoInvalido);
            }
            catch (Exception ex)
            {
                return Resultado<Documento>.Falha(ex.Message ?? DocumentoInvalido);
            }
        }

        

        public static Resultado<Documento> TentarCriarCpf(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var resultadoCpf = Cpf.TentarCriar(valor, configuracao);

            if (resultadoCpf.EhFalha)
                return Resultado<Documento>.Falha(resultadoCpf.Erros!);

            var cpf = resultadoCpf.Instancia;
            return Resultado<Documento>.Sucesso(
                new Documento(cpf.Valor, cpf: cpf, configuracao: configuracao));
        }

        

        public static Resultado<Documento> TentarCriarCnpj(
            string valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var resultadoCnpj = Cnpj.TentarCriar(valor, configuracao);

            if (resultadoCnpj.EhFalha)
                return Resultado<Documento>.Falha(resultadoCnpj.Erros!);

            var cnpj = resultadoCnpj.Instancia;
            return Resultado<Documento>.Sucesso(
                new Documento(cnpj.Valor, cnpj: cnpj, configuracao: configuracao));
        }

        public bool PossuiMesmoValor(Documento documento)
        {
            return Valor == documento.Valor;
        }

        public override string ToString()
        {
            return Formatado;
        }
    }
}
