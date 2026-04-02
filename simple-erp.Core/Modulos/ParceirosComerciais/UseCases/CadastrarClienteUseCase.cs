using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface ICadastrarClienteUseCase
        : IUseCase<CadastrarClienteEntrada, CadastrarClienteSaida>
    {
    }

    public record CadastrarClienteEntrada(
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

    public record CadastrarClienteSaida(
        long Id,
        string Documento,
        string Nome,
        string Email,
        bool Ativo
    );

    public sealed class CadastrarClienteUseCase : ICadastrarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CadastrarClienteUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<CadastrarClienteSaida>> ExecutarAsync(
            CadastrarClienteEntrada dados,
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
                return Resultado<CadastrarClienteSaida>.Falha(validacaoCampos.Erros!);
            
            var jaExiste = await _unitOfWork.ClientesRepository.ExistePorDocumentoAsync(
                resultadoDocumento.Instancia,
                cancellationToken);

            if (jaExiste.EhFalha) return Resultado<CadastrarClienteSaida>.Falha(jaExiste.Erros!);
            if (jaExiste.Instancia) return Resultado<CadastrarClienteSaida>.Falha("CLIENTE_JA_CADASTRADO");
            
            var resultadoCliente = Cliente.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                resultadoEndereco.Instancia);

            if (resultadoCliente.EhFalha)
                return Resultado<CadastrarClienteSaida>.Falha(resultadoCliente.Erros!);
            
            var cliente = resultadoCliente.Instancia;
            await _unitOfWork.ClientesRepository.AdicionarAsync(cliente, cancellationToken);

            var resSave = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (resSave.EhFalha) return Resultado<CadastrarClienteSaida>.Falha(resSave.Erros!);
            
            return Resultado<CadastrarClienteSaida>.Sucesso(new CadastrarClienteSaida(
                Id: cliente.Id.Valor,
                Documento: cliente.Documento.Formatado,
                Nome: cliente.Nome.Valor,
                Email: cliente.Email.Valor,
                Ativo: cliente.Ativo
            ));
        }
    }    
}
