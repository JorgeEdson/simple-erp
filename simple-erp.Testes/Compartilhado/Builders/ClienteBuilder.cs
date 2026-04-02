using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class ClienteBuilder
    {
        private long? _id = 202604020001;
        private string _nome = "Cliente Teste";
        private string _documento = "12345678909";
        private string _email = "cliente@teste.com";
        private Endereco _endereco = EnderecoBuilder.Novo().Criar();
        private bool _ativo = true;

        public static ClienteBuilder Novo() => new();

        public ClienteBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public ClienteBuilder SemId()
        {
            _id = null;
            return this;
        }

        public ClienteBuilder ComNome(string nome)
        {
            _nome = nome;
            return this;
        }

        public ClienteBuilder ComDocumento(string documento)
        {
            _documento = documento;
            return this;
        }

        public ClienteBuilder ComEmail(string email)
        {
            _email = email;
            return this;
        }

        public ClienteBuilder ComEndereco(Endereco endereco)
        {
            _endereco = endereco;
            return this;
        }

        public ClienteBuilder Ativo()
        {
            _ativo = true;
            return this;
        }

        public ClienteBuilder Inativo()
        {
            _ativo = false;
            return this;
        }

        public Cliente Criar()
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

            var resultadoCliente = Cliente.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                _endereco,
                _id);

            if (resultadoCliente.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar Cliente válido para o teste. Erros: {string.Join(", ", resultadoCliente.Erros!)}");

            var cliente = resultadoCliente.Instancia;

            if (!_ativo)
                cliente.Inativar();

            return cliente;
        }
    }
}
