using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IInativarFornecedorUseCase
       : IUseCase<InativarFornecedorEntrada, InativarFornecedorSaida>
    {
    }

    public record InativarFornecedorEntrada(long Id);

    public record InativarFornecedorSaida(
        long Id,
        bool Ativo
    );

    public sealed class InativarFornecedorUseCase : IInativarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public InativarFornecedorUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<InativarFornecedorSaida>> ExecutarAsync(
            InativarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<InativarFornecedorSaida>.Falha(resultadoId.Erros!);

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoFornecedor.EhFalha)
                return Resultado<InativarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
                return Resultado<InativarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");

            var resultadoInativacao = fornecedor.Inativar();

            if (resultadoInativacao.EhFalha)
                return Resultado<InativarFornecedorSaida>.Falha(resultadoInativacao.Erros!);

            var resultadoAtualizar = await _unitOfWork.FornecedoresRepository.AtualizarAsync(
                fornecedor,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<InativarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<InativarFornecedorSaida>.Falha(resultadoSave.Erros!);

            return Resultado<InativarFornecedorSaida>.Sucesso(
                new InativarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Ativo: fornecedor.Ativo));
        }
    }
}
