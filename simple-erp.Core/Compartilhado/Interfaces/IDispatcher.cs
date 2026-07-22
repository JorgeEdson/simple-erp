using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.Interfaces
{   
    public interface IDispatcher
    {
        Task<Resultado<TResposta>> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao,CancellationToken cancellationToken = default);
    }
}
