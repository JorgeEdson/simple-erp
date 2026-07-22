using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;

namespace simple_erp.Api.Mediador
{   
    public sealed class Dispatcher : IDispatcher
    {
        private readonly IServiceProvider _provedor;

        public Dispatcher(IServiceProvider provedor)
        {
            _provedor = provedor;
        }

        public async Task<Resultado<TResposta>> EnviarAsync<TResposta>(IRequisicao<TResposta> requisicao,CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requisicao);

            // Tipo concreto da entrada (ex.: CadastrarClienteEntrada) + tipo da resposta
            // formam o use case procurado: IUseCase<CadastrarClienteEntrada, CadastrarClienteSaida>.
            var tipoEntrada = requisicao.GetType();
            var tipoUseCase = typeof(IUseCase<,>).MakeGenericType(tipoEntrada, typeof(TResposta));

            var useCase = _provedor.GetService(tipoUseCase)
                ?? throw new InvalidOperationException(
                    $"Nenhum use case registrado para a requisição '{tipoEntrada.Name}' " +
                    $"com resposta '{typeof(TResposta).Name}'.");

            var metodo = tipoUseCase.GetMethod(nameof(IUseCase<IRequisicao<TResposta>, TResposta>.ExecutarAsync))
                ?? throw new InvalidOperationException(
                    $"Método ExecutarAsync não encontrado em '{tipoUseCase.Name}'.");

            var tarefa = (Task<Resultado<TResposta>>)metodo.Invoke(
                useCase,
                new object[] { requisicao, cancellationToken })!;

            return await tarefa;
        }
    }
}
