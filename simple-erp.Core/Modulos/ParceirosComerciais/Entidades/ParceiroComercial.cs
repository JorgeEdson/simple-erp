using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Entidades
{
    /// <summary>
    /// <b>Exemplo de Entity que NÃO é raiz de agregado.</b> É uma entidade (herda de
    /// <see cref="Entidade{TEntidade}"/>, tem identidade e comportamento), mas é abstrata,
    /// não possui repositório próprio e não é referenciada de fora do módulo. Por isso
    /// <b>não</b> implementa <see cref="IAggregateRoot"/>. Quem carrega essa marcação são
    /// as suas especializações concretas — <c>Cliente</c> e <c>Fornecedor</c> — que são as
    /// verdadeiras raízes de agregado, cada uma com seu próprio repositório.
    /// </summary>
    public abstract class ParceiroComercial : Entidade<ParceiroComercial>
    {
        protected ParceiroComercial()
        {
        }

        protected ParceiroComercial(
            Nome nome,
            Documento documento,
            Email email,
            Endereco endereco,
            bool ativo = true,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            Nome = nome;
            Documento = documento;
            Email = email;
            Endereco = endereco;
            Ativo = ativo;
        }

        public Nome Nome { get; protected set; }
        public Documento Documento { get; protected set; }
        public Email Email { get; protected set; }
        public Endereco Endereco { get; protected set; }
        public bool Ativo { get; protected set; }

        public Resultado<bool> AlterarNome(Nome nome)
        {
            if (nome is null)
                return Resultado<bool>.Falha("NOME_OBRIGATORIO");

            if (Nome.IgualA(nome))
                return Resultado<bool>.Sucesso(true);

            Nome = nome;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AlterarDocumento(Documento documento)
        {
            if (documento is null)
                return Resultado<bool>.Falha("DOCUMENTO_OBRIGATORIO");

            if (Documento.IgualA(documento))
                return Resultado<bool>.Sucesso(true);

            Documento = documento;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AlterarEmail(Email email)
        {
            if (email is null)
                return Resultado<bool>.Falha("EMAIL_OBRIGATORIO");

            if (Email.IgualA(email))
                return Resultado<bool>.Sucesso(true);

            Email = email;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AlterarEndereco(Endereco endereco)
        {
            if (endereco is null)
                return Resultado<bool>.Falha("ENDERECO_OBRIGATORIO");

            if (Endereco.IgualA(endereco))
                return Resultado<bool>.Sucesso(true);

            Endereco = endereco;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public virtual Resultado<bool> Ativar()
        {
            if (Ativo)
                return Resultado<bool>.Sucesso(true);

            Ativo = true;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public virtual Resultado<bool> Inativar()
        {
            if (!Ativo)
                return Resultado<bool>.Sucesso(true);

            Ativo = false;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }
    }
}
