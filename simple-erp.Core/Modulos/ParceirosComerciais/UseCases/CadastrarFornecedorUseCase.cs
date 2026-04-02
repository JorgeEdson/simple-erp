using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface ICadastrarFornecedorUseCase
        : IUseCase<CadastrarFornecedorEntrada, CadastrarFornecedorSaida>
    {
    }

    public record CadastrarFornecedorEntrada(
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

    public record CadastrarFornecedorSaida(
        long Id,
        string Documento,
        string Nome,
        string Email,
        bool Ativo
    );

    public sealed class CadastrarFornecedorUseCase : ICadastrarFornecedorUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CadastrarFornecedorUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<CadastrarFornecedorSaida>> ExecutarAsync(
            CadastrarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoDocumento = Documento.TentarCriar(dados.Documento);
            var resultadoNome = Nome.TentarCriar(dados.Nome);
            var resultadoEmail = Email.TentarCriar(dados.Email);
            var resultadoEndereco = Endereco.TentarCriar(new PropriedadesEndereco(
                dados.Rua, dados.Numero, dados.Complemento, dados.Bairro,
                dados.Cidade, dados.Estado, dados.Cep, dados.Pais));

            var validacaoCampos = Resultado.Combinar(resultadoDocumento, resultadoNome, resultadoEmail, resultadoEndereco);

            if (validacaoCampos.EhFalha)
                return Resultado<CadastrarFornecedorSaida>.Falha(validacaoCampos.Erros!);

            var jaExiste = await _unitOfWork.FornecedoresRepository.ExistePorDocumentoAsync(
                resultadoDocumento.Instancia,
                cancellationToken);

            if (jaExiste.EhFalha) return Resultado<CadastrarFornecedorSaida>.Falha(jaExiste.Erros!);
            if (jaExiste.Instancia) return Resultado<CadastrarFornecedorSaida>.Falha("FORNECEDOR_JA_CADASTRADO");

            var resultadoFornecedor = Fornecedor.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                resultadoEndereco.Instancia);

            if (resultadoFornecedor.EhFalha)
                return Resultado<CadastrarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);

            var fornecedor = resultadoFornecedor.Instancia;
            await _unitOfWork.FornecedoresRepository.AdicionarAsync(fornecedor, cancellationToken);

            var resSave = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (resSave.EhFalha) return Resultado<CadastrarFornecedorSaida>.Falha(resSave.Erros!);

            return Resultado<CadastrarFornecedorSaida>.Sucesso(new CadastrarFornecedorSaida(
                Id: fornecedor.Id.Valor,
                Documento: fornecedor.Documento.Formatado,
                Nome: fornecedor.Nome.Valor,
                Email: fornecedor.Email.Valor,
                Ativo: fornecedor.Ativo
            ));
        }
    }
}
