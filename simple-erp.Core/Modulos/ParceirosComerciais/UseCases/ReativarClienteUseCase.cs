using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IReativarClienteUseCase : IUseCase<ReativarClienteEntrada, ReativarClienteSaida>
    {
    }

    public sealed record ReativarClienteEntrada(long Id);

    public sealed record ReativarClienteSaida(
       long Id,
       bool Ativo);

    public sealed class ReativarClienteUseCase : IReativarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReativarClienteUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<ReativarClienteSaida>> ExecutarAsync(
            ReativarClienteEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<ReativarClienteSaida>.Falha(resultadoId.Erros!);

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoCliente.EhFalha)
                return Resultado<ReativarClienteSaida>.Falha(resultadoCliente.Erros!);

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
                return Resultado<ReativarClienteSaida>.Falha("CLIENTE_NAO_ENCONTRADO");

            var resultadoAtivacao = cliente.Ativar();

            if (resultadoAtivacao.EhFalha)
                return Resultado<ReativarClienteSaida>.Falha(resultadoAtivacao.Erros!);

            var resultadoAtualizar = await _unitOfWork.ClientesRepository.AtualizarAsync(
                cliente,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<ReativarClienteSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<ReativarClienteSaida>.Falha(resultadoSave.Erros!);

            return Resultado<ReativarClienteSaida>.Sucesso(
                new ReativarClienteSaida(
                    Id: cliente.Id.Valor,
                    Ativo: cliente.Ativo));
        }
    }
}
