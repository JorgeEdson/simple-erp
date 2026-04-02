using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IEditarClienteUseCase
         : IUseCase<EditarClienteEntrada, EditarClienteSaida>
    {
    }

    public record EditarClienteEntrada(
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

    public record EditarClienteSaida(
        long Id,
        string Documento,
        string Nome,
        string Email,
        bool Ativo
    );
    public sealed class EditarClienteUseCase : IEditarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public EditarClienteUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<EditarClienteSaida>> ExecutarAsync(
            EditarClienteEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);
            var resultadoDocumento = Documento.TentarCriar(dados.Documento);
            var resultadoNome = Nome.TentarCriar(dados.Nome);
            var resultadoEmail = Email.TentarCriar(dados.Email);
            var resultadoEndereco = Endereco.TentarCriar(
                new PropriedadesEndereco(
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
                return Resultado<EditarClienteSaida>.Falha(validacaoCampos.Erros!);

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoCliente.EhFalha)
                return Resultado<EditarClienteSaida>.Falha(resultadoCliente.Erros!);

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
                return Resultado<EditarClienteSaida>.Falha("CLIENTE_NAO_ENCONTRADO");

            var documentoFoiAlterado = !cliente.Documento.IgualA(resultadoDocumento.Instancia);

            if (documentoFoiAlterado)
            {
                var resultadoExisteDocumento = await _unitOfWork.ClientesRepository.ExisteOutroPorDocumentoAsync(cliente.Id, resultadoDocumento.Instancia, cancellationToken);

                if (resultadoExisteDocumento.EhFalha)
                    return Resultado<EditarClienteSaida>.Falha(resultadoExisteDocumento.Erros!);

                if (resultadoExisteDocumento.Instancia)
                    return Resultado<EditarClienteSaida>.Falha("CLIENTE_JA_CADASTRADO");
            }

            var resultadoAlterarNome = cliente.AlterarNome(resultadoNome.Instancia);
            var resultadoAlterarDocumento = cliente.AlterarDocumento(resultadoDocumento.Instancia);
            var resultadoAlterarEmail = cliente.AlterarEmail(resultadoEmail.Instancia);
            var resultadoAlterarEndereco = cliente.AlterarEndereco(resultadoEndereco.Instancia);

            var resultadoAlteracoes = Resultado.Combinar(
                resultadoAlterarNome,
                resultadoAlterarDocumento,
                resultadoAlterarEmail,
                resultadoAlterarEndereco);

            if (resultadoAlteracoes.EhFalha)
                return Resultado<EditarClienteSaida>.Falha(resultadoAlteracoes.Erros!);

            var resultadoAtualizar = await _unitOfWork.ClientesRepository.AtualizarAsync(
                cliente,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<EditarClienteSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<EditarClienteSaida>.Falha(resultadoSave.Erros!);

            return Resultado<EditarClienteSaida>.Sucesso(
                new EditarClienteSaida(
                    Id: cliente.Id.Valor,
                    Documento: cliente.Documento.Formatado,
                    Nome: cliente.Nome.Valor,
                    Email: cliente.Email.Valor,
                    Ativo: cliente.Ativo));
        }
    }
}
