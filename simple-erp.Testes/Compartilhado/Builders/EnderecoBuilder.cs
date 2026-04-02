using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class EnderecoBuilder
    {
        private string _rua = "Rua das Flores";
        private string _numero = "123";
        private string _complemento = "Apto 101";
        private string _bairro = "Centro";
        private string _cidade = "Fortaleza";
        private string _estado = "CE";
        private string _cep = "60000-000";
        private string _pais = "Brasil";

        public static EnderecoBuilder Novo() => new();

        public EnderecoBuilder ComRua(string rua)
        {
            _rua = rua;
            return this;
        }

        public EnderecoBuilder ComNumero(string numero)
        {
            _numero = numero;
            return this;
        }

        public EnderecoBuilder ComComplemento(string complemento)
        {
            _complemento = complemento;
            return this;
        }

        public EnderecoBuilder ComBairro(string bairro)
        {
            _bairro = bairro;
            return this;
        }

        public EnderecoBuilder ComCidade(string cidade)
        {
            _cidade = cidade;
            return this;
        }

        public EnderecoBuilder ComEstado(string estado)
        {
            _estado = estado;
            return this;
        }

        public EnderecoBuilder ComCep(string cep)
        {
            _cep = cep;
            return this;
        }

        public EnderecoBuilder ComPais(string pais)
        {
            _pais = pais;
            return this;
        }

        public PropriedadesEndereco ConstruirPropriedades()
        {
            return new PropriedadesEndereco(
                Rua: _rua,
                Numero: _numero,
                Complemento: _complemento,
                Bairro: _bairro,
                Cidade: _cidade,
                Estado: _estado,
                Cep: _cep,
                Pais: _pais);
        }

        public Endereco Criar()
        {
            var resultado = Endereco.TentarCriar(ConstruirPropriedades());

            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar Endereco válido para o teste. Erros: {string.Join(", ", resultado.Erros!)}");

            return resultado.Instancia;
        }
    }
}
