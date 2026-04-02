using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class FornecedorBuilder
    {
        private long? _id = 202604020002;
        private string _nome = "Fornecedor Teste";
        private string _documento = "11222333000181";
        private string _email = "fornecedor@teste.com";
        private Endereco _endereco = EnderecoBuilder.Novo().Criar();
        private bool _ativo = true;

        public static FornecedorBuilder Novo() => new();

        public FornecedorBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public FornecedorBuilder SemId()
        {
            _id = null;
            return this;
        }

        public FornecedorBuilder ComNome(string nome)
        {
            _nome = nome;
            return this;
        }

        public FornecedorBuilder ComDocumento(string documento)
        {
            _documento = documento;
            return this;
        }

        public FornecedorBuilder ComEmail(string email)
        {
            _email = email;
            return this;
        }

        public FornecedorBuilder ComEndereco(Endereco endereco)
        {
            _endereco = endereco;
            return this;
        }

        public FornecedorBuilder Ativo()
        {
            _ativo = true;
            return this;
        }

        public FornecedorBuilder Inativo()
        {
            _ativo = false;
            return this;
        }

        public Fornecedor Criar()
        {
            var resultadoNome = Nome.TentarCriar(_nome);
            var resultadoDocumento = Documento.TentarCriar(_documento);
            var resultadoEmail = Email.TentarCriar(_email);

            if (resultadoNome.EhFalha)
                throw new InvalidOperationException($"Nome inválido no builder: {string.Join(", ", resultadoNome.Erros!)}");

            if (resultadoDocumento.EhFalha)
                throw new InvalidOperationException($"Documento inválido no builder: {string.Join(", ", resultadoDocumento.Erros!)}");

            if (resultadoEmail.EhFalha)
                throw new InvalidOperationException($"Email inválido no builder: {string.Join(", ", resultadoEmail.Erros!)}");

            var resultadoFornecedor = Fornecedor.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                _endereco,
                _id);

            if (resultadoFornecedor.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar Fornecedor válido para o teste. Erros: {string.Join(", ", resultadoFornecedor.Erros!)}");

            var fornecedor = resultadoFornecedor.Instancia;

            if (!_ativo)
                fornecedor.Inativar();

            return fornecedor;
        }
    }
}
