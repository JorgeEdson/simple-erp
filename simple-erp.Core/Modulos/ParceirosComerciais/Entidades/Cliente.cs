
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Eventos;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Entidades
{
    public sealed class Cliente : ParceiroComercial
    {
        private Cliente(
            Nome nome,
            Documento documento,
            Email email,
            Endereco endereco,
            bool ativo = true,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(
                nome,
                documento,
                email,
                endereco,
                ativo,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc)
        {
        }

        public static Resultado<Cliente> Criar(
            Nome nome,
            Documento documento,
            Email email,
            Endereco endereco,
            long? id = null)
        {
            var erros = new List<string>();

            if (nome is null)
                erros.Add("NOME_OBRIGATORIO");

            if (documento is null)
                erros.Add("DOCUMENTO_OBRIGATORIO");

            if (email is null)
                erros.Add("EMAIL_OBRIGATORIO");

            if (endereco is null)
                erros.Add("ENDERECO_OBRIGATORIO");

            if (erros.Any())
                return Resultado<Cliente>.Falha(erros);

            var cliente = new Cliente(
                nome!,
                documento!,
                email!,
                endereco!,
                true,
                id);

            cliente.AdicionarEventoDeDominio(
                new ClienteCadastrado(cliente.Id, cliente.Documento, cliente.Nome));

            return Resultado<Cliente>.Sucesso(cliente);
        }

        public override Resultado<bool> Ativar()
        {
            if (Ativo)
                return Resultado<bool>.Sucesso(true);

            var resultado = base.Ativar();

            if (resultado.EhFalha)
                return resultado;

            AdicionarEventoDeDominio(new ClienteReativado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public override Resultado<bool> Inativar()
        {
            if (!Ativo)
                return Resultado<bool>.Sucesso(true);

            var resultado = base.Inativar();

            if (resultado.EhFalha)
                return resultado;

            AdicionarEventoDeDominio(new ClienteInativado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public static Cliente Reconstituir(
            Nome nome,
            Documento documento,
            Email email,
            Endereco endereco,
            bool ativo,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new Cliente(
                nome,
                documento,
                email,
                endereco,
                ativo,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
