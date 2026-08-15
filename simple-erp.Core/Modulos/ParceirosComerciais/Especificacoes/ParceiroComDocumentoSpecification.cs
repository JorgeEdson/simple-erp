using simple_erp.Core.Compartilhado.Contratos.Dominio.Especificacoes;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Linq.Expressions;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Especificacoes
{
    /// <summary>
    /// Regra: "o parceiro possui exatamente este documento". Sozinha, expressa a unicidade
    /// de documento no cadastro; combinada com <see cref="ParceiroDiferenteDeSpecification{TParceiro}"/>
    /// via <c>And</c>, expressa a unicidade na edição ("existe OUTRO com este documento").
    ///
    /// <para>
    /// A expressão compara o Value Object <c>Documento</c> inteiro (<c>parceiro.Documento == documento</c>),
    /// exatamente a forma que o mapeamento do EF Core sabe traduzir para a coluna.
    /// </para>
    /// </summary>
    public sealed class ParceiroComDocumentoSpecification<TParceiro> : Specification<TParceiro>
        where TParceiro : ParceiroComercial
    {
        private readonly Documento _documento;

        public ParceiroComDocumentoSpecification(Documento documento)
        {
            _documento = documento;
        }

        public override Expression<Func<TParceiro, bool>> ParaExpressao() =>
            parceiro => parceiro.Documento == _documento;
    }
}
