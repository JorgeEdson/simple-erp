using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Especificacoes;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IEditarClienteUseCase
         : IUseCase<EditarClienteEntrada, EditarClienteSaida>
    {
    }

    public record EditarClienteEntrada(
        Guid Id,
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
    ) : IRequisicao<EditarClienteSaida>;

    public record EditarClienteSaida(
        Guid Id,
        string Documento,
        string Nome,
        string Email,
        bool Ativo
    );

    public sealed class EditarClienteUseCase : IEditarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public EditarClienteUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<EditarClienteSaida>> ExecutarAsync(EditarClienteEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(EditarClienteUseCase),
                ["ClienteId"] = dados.Id,
                ["Nome"] = dados.Nome,
                ["Email"] = dados.Email
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando edição de cliente."));

            #endregion

            #region Validação do identificador

            if (dados.Id == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Id"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Validação da entrada

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

            var validacaoCampos = Resultado.Combinar(resultadoDocumento,
                resultadoNome,
                resultadoEmail,
                resultadoEndereco);

            if (validacaoCampos.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para edição de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha(validacaoCampos.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterCliente = Stopwatch.StartNew();

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                dados.Id,
                cancellationToken);

            stopwatchObterCliente.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de cliente por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObterCliente.ElapsedMilliseconds
                }));

            if (resultadoCliente.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter cliente por id para edição.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = dados.Id,
                        ["Erros"] = resultadoCliente.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha(resultadoCliente.Erros!);
            }

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de edição de cliente não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha("CLIENTE_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            var documentoFoiAlterado = !cliente.Documento.IgualA(resultadoDocumento.Instancia);

            if (documentoFoiAlterado)
            {
                var stopwatchExisteOutroDocumento = Stopwatch.StartNew();

                // "Existe OUTRO cliente com este documento" = mesma regra de documento
                // COMPOSTA com "id diferente do que estou editando". A composição substitui
                // o antigo método dedicado ExisteOutroPorDocumento.
                var especificacaoOutroComDocumento =
                    new ParceiroComDocumentoSpecification<Cliente>(resultadoDocumento.Instancia)
                        .And(new ParceiroDiferenteDeSpecification<Cliente>(cliente.Id));

                var resultadoExisteDocumento = await _unitOfWork.ClientesRepository.ExisteAsync(
                    especificacaoOutroComDocumento,
                    cancellationToken);

                stopwatchExisteOutroDocumento.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Verificação de duplicidade de documento concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoRepositorio"] = "ExisteAsync(ComDocumento.And(DiferenteDe))",
                        ["DuracaoMs"] = stopwatchExisteOutroDocumento.ElapsedMilliseconds
                    }));

                if (resultadoExisteDocumento.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao verificar duplicidade de documento na edição de cliente.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ClienteId"] = cliente.Id,
                            ["Documento"] = resultadoDocumento.Instancia.Formatado,
                            ["Erros"] = resultadoExisteDocumento.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarClienteSaida>.Falha(resultadoExisteDocumento.Erros!);
                }

                if (resultadoExisteDocumento.Instancia)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Tentativa de edição de cliente com documento já cadastrado para outro cliente.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ClienteId"] = cliente.Id,
                            ["Documento"] = resultadoDocumento.Instancia.Formatado,
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarClienteSaida>.Falha("CLIENTE_JA_CADASTRADO");
                }
            }

            #endregion

            #region Execução das regras de negócio

                #region Alteração dos dados do cliente

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
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao aplicar alterações no agregado Cliente.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ClienteId"] = cliente.Id,
                            ["Erros"] = resultadoAlteracoes.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarClienteSaida>.Falha(resultadoAlteracoes.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.ClientesRepository.AtualizarAsync(
                cliente,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de cliente no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar cliente no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = cliente.Id,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da edição de cliente concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir edição de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = cliente.Id,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarClienteSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Cliente editado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ClienteId"] = cliente.Id,
                    ["Ativo"] = cliente.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<EditarClienteSaida>.Sucesso(
                new EditarClienteSaida(
                    Id: cliente.Id,
                    Documento: cliente.Documento.Formatado,
                    Nome: cliente.Nome.Valor,
                    Email: cliente.Email.Valor,
                    Ativo: cliente.Ativo));

            #endregion
        }
    }
}
