using simple_erp.Core.Compartilhado.Contratos.Dominio.Especificacoes;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using System.Linq.Expressions;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Especificacoes
{
    /// <summary>
    /// Regra: "o parceiro é diferente deste (id distinto)". Existe para ser composta: ao
    /// combinar com <see cref="ParceiroComDocumentoSpecification{TParceiro}"/> por <c>And</c>,
    /// obtém-se "existe outro parceiro com o mesmo documento" — a checagem de duplicidade na
    /// edição, sem precisar de um método dedicado no repositório.
    /// </summary>
    public sealed class ParceiroDiferenteDeSpecification<TParceiro> : Specification<TParceiro>
        where TParceiro : ParceiroComercial
    {
        private readonly Guid _id;

        public ParceiroDiferenteDeSpecification(Guid id)
        {
            _id = id;
        }

        public override Expression<Func<TParceiro, bool>> ParaExpressao() =>
            parceiro => parceiro.Id != _id;
    }
}
