using System.Linq.Expressions;

namespace simple_erp.Core.Compartilhado.Especificacoes
{
    /// <summary>
    /// Base para especificações concretas. A subclasse só precisa fornecer a expressão da
    /// regra em <see cref="ParaExpressao"/>; a avaliação em memória e os combinadores
    /// (And/Or/Nao) já vêm prontos aqui.
    /// </summary>
    public abstract class Specification<T> : ISpecification<T>
    {
        public abstract Expression<Func<T, bool>> ParaExpressao();

        public bool EhSatisfeitaPor(T candidato) => ParaExpressao().Compile().Invoke(candidato);

        public ISpecification<T> And(ISpecification<T> outra) => new AndSpecification<T>(this, outra);
        public ISpecification<T> Or(ISpecification<T> outra) => new OrSpecification<T>(this, outra);
        public ISpecification<T> Nao() => new NotSpecification<T>(this);
    }
}
