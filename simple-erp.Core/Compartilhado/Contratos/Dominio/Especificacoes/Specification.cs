using simple_erp.Core.Compartilhado.Contratos.Dominio;
using System.Linq.Expressions;

namespace simple_erp.Core.Compartilhado.Contratos.Dominio.Especificacoes
{  
    public abstract class Specification<T> : ISpecification<T>
    {
        public abstract Expression<Func<T, bool>> ParaExpressao();

        public bool EhSatisfeitaPor(T candidato) => ParaExpressao().Compile().Invoke(candidato);

        public ISpecification<T> And(ISpecification<T> outra) => new AndSpecification<T>(this, outra);
        public ISpecification<T> Or(ISpecification<T> outra) => new OrSpecification<T>(this, outra);
        public ISpecification<T> Nao() => new NotSpecification<T>(this);
    }
}
