using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IEditarFornecedorUseCase
        : IUseCase<EditarFornecedorEntrada, EditarFornecedorSaida>
    {
    }

    public record EditarFornecedorEntrada(
        long Id,
        string Documento,
        string Nome,
        string Email,
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string Pais
    );

    public record EditarFornecedorSaida(
        long Id,
        string Documento,
        string Nome,
        string Email,
        bool Ativo
    );

    public sealed class EditarFornecedorUseCase : IEditarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public EditarFornecedorUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<EditarFornecedorSaida>> ExecutarAsync(
            EditarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);
            var resultadoDocumento = Documento.TentarCriar(dados.Documento);
            var resultadoNome = Nome.TentarCriar(dados.Nome);
            var resultadoEmail = Email.TentarCriar(dados.Email);
            var resultadoEndereco = Endereco.TentarCriar(new PropriedadesEndereco(
                dados.Rua,
                dados.Numero,
                dados.Complemento,
                dados.Bairro,
                dados.Cidade,
                dados.Estado,
                dados.Cep,
                dados.Pais));

            var validacaoCampos = Resultado.Combinar(
                resultadoId,
                resultadoDocumento,
                resultadoNome,
                resultadoEmail,
                resultadoEndereco);

            if (validacaoCampos.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(validacaoCampos.Erros!);

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoFornecedor.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
                return Resultado<EditarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");

            var resultadoExisteOutroDocumento = await _unitOfWork.FornecedoresRepository.ExisteOutroPorDocumentoAsync(
                resultadoId.Instancia,
                resultadoDocumento.Instancia,
                cancellationToken);

            if (resultadoExisteOutroDocumento.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(resultadoExisteOutroDocumento.Erros!);

            if (resultadoExisteOutroDocumento.Instancia)
                return Resultado<EditarFornecedorSaida>.Falha("DOCUMENTO_JA_CADASTRADO");

            var resultadoAlterarDocumento = fornecedor.AlterarDocumento(resultadoDocumento.Instancia);
            var resultadoAlterarNome = fornecedor.AlterarNome(resultadoNome.Instancia);
            var resultadoAlterarEmail = fornecedor.AlterarEmail(resultadoEmail.Instancia);
            var resultadoAlterarEndereco = fornecedor.AlterarEndereco(resultadoEndereco.Instancia);

            var resultadoAlteracoes = Resultado.Combinar(
                resultadoAlterarDocumento,
                resultadoAlterarNome,
                resultadoAlterarEmail,
                resultadoAlterarEndereco);

            if (resultadoAlteracoes.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(resultadoAlteracoes.Erros!);

            var resultadoAtualizar = await _unitOfWork.FornecedoresRepository.AtualizarAsync(
                fornecedor,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<EditarFornecedorSaida>.Falha(resultadoSave.Erros!);

            return Resultado<EditarFornecedorSaida>.Sucesso(
                new EditarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Documento: fornecedor.Documento.Formatado,
                    Nome: fornecedor.Nome.Valor,
                    Email: fornecedor.Email.Valor,
                    Ativo: fornecedor.Ativo));
        }
    }
}
