using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    /// <summary>
    /// Contrato de um assinante (handler) de um evento de domínio. Um mesmo evento
    /// pode ter vários handlers, em módulos diferentes (fan-out), sem que o produtor
    /// os conheça. É a peça específica de cada contexto que reage a um fato.
    /// </summary>
    public interface IManipuladorDeEventoDeDominio<in TEvento>
        where TEvento : EventoDeDominio
    {
        Task<Resultado<bool>> ManipularAsync(TEvento evento, CancellationToken cancellationToken = default);
    }
}
