using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IReativarFornecedorUseCase
        : IUseCase<ReativarFornecedorEntrada, ReativarFornecedorSaida>
    {
    }

    public record ReativarFornecedorEntrada(long Id);

    public record ReativarFornecedorSaida(
        long Id,
        bool Ativo
    );   

    public sealed class ReativarFornecedorUseCase : IReativarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReativarFornecedorUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<ReativarFornecedorSaida>> ExecutarAsync(
            ReativarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<ReativarFornecedorSaida>.Falha(resultadoId.Erros!);

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoFornecedor.EhFalha)
                return Resultado<ReativarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
                return Resultado<ReativarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");

            var resultadoAtivacao = fornecedor.Ativar();

            if (resultadoAtivacao.EhFalha)
                return Resultado<ReativarFornecedorSaida>.Falha(resultadoAtivacao.Erros!);

            var resultadoAtualizar = await _unitOfWork.FornecedoresRepository.AtualizarAsync(
                fornecedor,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<ReativarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<ReativarFornecedorSaida>.Falha(resultadoSave.Erros!);

            return Resultado<ReativarFornecedorSaida>.Sucesso(
                new ReativarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Ativo: fornecedor.Ativo));
        }
    }
}
