using simple_erp.Core.Compartilhado.Contratos.Dominio;
using System.Linq.Expressions;

namespace simple_erp.Core.Compartilhado.Contratos.Dominio.Especificacoes
{  
    internal static class CombinadorDeExpressoes
    {
        public static Expression<Func<T, bool>> Combinar<T>(
            Expression<Func<T, bool>> esquerda,
            Expression<Func<T, bool>> direita,
            Func<Expression, Expression, Expression> juntar)
        {
            var parametro = Expression.Parameter(typeof(T));

            var corpoEsquerda = new SubstituidorDeParametro(esquerda.Parameters[0], parametro)
                .Visit(esquerda.Body)!;
            var corpoDireita = new SubstituidorDeParametro(direita.Parameters[0], parametro)
                .Visit(direita.Body)!;

            return Expression.Lambda<Func<T, bool>>(juntar(corpoEsquerda, corpoDireita), parametro);
        }

        private sealed class SubstituidorDeParametro : ExpressionVisitor
        {
            private readonly ParameterExpression _de;
            private readonly Expression _para;

            public SubstituidorDeParametro(ParameterExpression de, Expression para)
            {
                _de = de;
                _para = para;
            }

            protected override Expression VisitParameter(ParameterExpression node) =>
                node == _de ? _para : base.VisitParameter(node);
        }
    }

    internal sealed class AndSpecification<T> : Specification<T>
    {
        private readonly ISpecification<T> _esquerda;
        private readonly ISpecification<T> _direita;

        public AndSpecification(ISpecification<T> esquerda, ISpecification<T> direita)
        {
            _esquerda = esquerda;
            _direita = direita;
        }

        public override Expression<Func<T, bool>> ParaExpressao() =>
            CombinadorDeExpressoes.Combinar(
                _esquerda.ParaExpressao(),
                _direita.ParaExpressao(),
                Expression.AndAlso);
    }

    internal sealed class OrSpecification<T> : Specification<T>
    {
        private readonly ISpecification<T> _esquerda;
        private readonly ISpecification<T> _direita;

        public OrSpecification(ISpecification<T> esquerda, ISpecification<T> direita)
        {
            _esquerda = esquerda;
            _direita = direita;
        }

        public override Expression<Func<T, bool>> ParaExpressao() =>
            CombinadorDeExpressoes.Combinar(
                _esquerda.ParaExpressao(),
                _direita.ParaExpressao(),
                Expression.OrElse);
    }

    internal sealed class NotSpecification<T> : Specification<T>
    {
        private readonly ISpecification<T> _especificacao;

        public NotSpecification(ISpecification<T> especificacao)
        {
            _especificacao = especificacao;
        }

        public override Expression<Func<T, bool>> ParaExpressao()
        {
            var expressao = _especificacao.ParaExpressao();
            var corpoNegado = Expression.Not(expressao.Body);
            return Expression.Lambda<Func<T, bool>>(corpoNegado, expressao.Parameters);
        }
    }
}
