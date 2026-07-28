using System.Linq.Expressions;

namespace simple_erp.Core.Compartilhado.Especificacoes
{
    /// <summary>
    /// <b>Specification</b> (padrão tático do DDD): encapsula uma regra de negócio na forma
    /// de um predicado de primeira classe — "este candidato satisfaz a regra?".
    ///
    /// <para>
    /// A regra é exposta de duas formas complementares, cobrindo os dois usos clássicos do
    /// padrão:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///   <see cref="EhSatisfeitaPor"/> — avaliação <b>em memória</b> sobre um objeto já
    ///   carregado (validação).
    ///   </description></item>
    ///   <item><description>
    ///   <see cref="ParaExpressao"/> — a mesma regra como <see cref="Expression"/>, que o
    ///   repositório traduz para <b>consulta</b> (SQL via EF Core).
    ///   </description></item>
    /// </list>
    ///
    /// <para>
    /// O ganho central do padrão é a <b>composição</b>: regras pequenas se combinam com
    /// <see cref="And"/>, <see cref="Or"/> e <see cref="Nao"/> sem duplicação. É o que
    /// transforma "existe parceiro com este documento" e "existe OUTRO parceiro com este
    /// documento" (mesma regra + "id diferente") em uma composição, em vez de dois métodos
    /// quase idênticos.
    /// </para>
    /// </summary>
    public interface ISpecification<T>
    {
        /// <summary>A regra como expressão, para ser traduzida em consulta pelo repositório.</summary>
        Expression<Func<T, bool>> ParaExpressao();

        /// <summary>Avalia a regra em memória, sobre um candidato já carregado.</summary>
        bool EhSatisfeitaPor(T candidato);

        /// <summary>Combina esta regra com outra por E lógico.</summary>
        ISpecification<T> And(ISpecification<T> outra);

        /// <summary>Combina esta regra com outra por OU lógico.</summary>
        ISpecification<T> Or(ISpecification<T> outra);

        /// <summary>Nega esta regra.</summary>
        ISpecification<T> Nao();
    }
}
