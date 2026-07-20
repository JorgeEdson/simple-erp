using System;
using System.Collections.Generic;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    /// <summary>
    /// Resolve os handlers registrados para um tipo de evento. A implementação usada
    /// em produção é normalmente respaldada por injeção de dependência (o container
    /// resolve todos os IManipuladorDeEventoDeDominio&lt;TEvento&gt;). Em testes e no demo,
    /// usa-se um resolvedor em memória.
    /// </summary>
    public interface IResolvedorDeManipuladores
    {
        IReadOnlyCollection<object> ResolverPara(Type tipoDoEvento);
    }
}
