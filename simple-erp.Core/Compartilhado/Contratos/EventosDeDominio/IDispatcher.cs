using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Compartilhado.Contratos.EventosDeDominio
{   
    public interface IDispatcher
    {
        Task<Resultado<TResposta>> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao,CancellationToken cancellationToken = default);
    }
}
