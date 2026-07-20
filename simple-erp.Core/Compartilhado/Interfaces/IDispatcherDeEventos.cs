using simple_erp.Core.Compartilhado.Base;
using System.Collections.Generic;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    /// <summary>
    /// Entrega os eventos de domínio produzidos por um agregado aos seus handlers.
    /// É uma peça única e genérica de infraestrutura — não conhece nenhum módulo em
    /// particular. Tipicamente acionado após a persistência do agregado produtor.
    /// </summary>
    public interface IDispatcherDeEventos
    {
        Task<Resultado<bool>> DespacharAsync(
            IEnumerable<EventoDeDominio> eventos,
            CancellationToken cancellationToken = default);
    }
}
