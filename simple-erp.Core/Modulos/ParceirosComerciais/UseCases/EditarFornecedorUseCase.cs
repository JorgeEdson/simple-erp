using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Diagnostics;

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
    ) : IRequisicao<EditarFornecedorSaida>;

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
        private readonly ILogService _logService;

        public EditarFornecedorUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<EditarFornecedorSaida>> ExecutarAsync(EditarFornecedorEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(EditarFornecedorUseCase),
                ["FornecedorId"] = dados.Id,
                ["Nome"] = dados.Nome,
                ["Email"] = dados.Email
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando edição de fornecedor."));

            #endregion

            #region Validação da entrada

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
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para edição de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarFornecedorSaida>.Falha(validacaoCampos.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterFornecedor = Stopwatch.StartNew();

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            stopwatchObterFornecedor.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de fornecedor por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObterFornecedor.ElapsedMilliseconds
                }));

            if (resultadoFornecedor.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter fornecedor por id para edição.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);
            }

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de edição de fornecedor não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarFornecedorSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            var documentoFoiAlterado = !fornecedor.Documento.IgualA(resultadoDocumento.Instancia);

            if (documentoFoiAlterado)
            {
                var stopwatchExisteOutroDocumento = Stopwatch.StartNew();

                var resultadoExisteOutroDocumento = await _unitOfWork.FornecedoresRepository.ExisteOutroPorDocumentoAsync(
                    fornecedor.Id,
                    resultadoDocumento.Instancia,
                    cancellationToken);

                stopwatchExisteOutroDocumento.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Verificação de duplicidade de documento concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoRepositorio"] = "ExisteOutroPorDocumentoAsync",
                        ["DuracaoMs"] = stopwatchExisteOutroDocumento.ElapsedMilliseconds
                    }));

                if (resultadoExisteOutroDocumento.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao verificar duplicidade de documento na edição de fornecedor.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["FornecedorId"] = fornecedor.Id.Valor,
                            ["Documento"] = resultadoDocumento.Instancia.Formatado,
                            ["Erros"] = resultadoExisteOutroDocumento.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarFornecedorSaida>.Falha(resultadoExisteOutroDocumento.Erros!);
                }

                if (resultadoExisteOutroDocumento.Instancia)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Tentativa de edição de fornecedor com documento já cadastrado para outro fornecedor.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["FornecedorId"] = fornecedor.Id.Valor,
                            ["Documento"] = resultadoDocumento.Instancia.Formatado,
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarFornecedorSaida>.Falha("DOCUMENTO_JA_CADASTRADO");
                }
            }

            #endregion

            #region Execução das regras de negócio

                #region Alteração dos dados do fornecedor

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
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao aplicar alterações no agregado Fornecedor.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["FornecedorId"] = fornecedor.Id.Valor,
                            ["Erros"] = resultadoAlteracoes.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarFornecedorSaida>.Falha(resultadoAlteracoes.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.FornecedoresRepository.AtualizarAsync(
                fornecedor,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de fornecedor no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar fornecedor no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarFornecedorSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da edição de fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir edição de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarFornecedorSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Fornecedor editado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["FornecedorId"] = fornecedor.Id.Valor,
                    ["Ativo"] = fornecedor.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<EditarFornecedorSaida>.Sucesso(
                new EditarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Documento: fornecedor.Documento.Formatado,
                    Nome: fornecedor.Nome.Valor,
                    Email: fornecedor.Email.Valor,
                    Ativo: fornecedor.Ativo));

            #endregion
        }
    }
}
