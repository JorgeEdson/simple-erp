using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Diagnostics;

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
        private readonly ILogService _logService;

        public CadastrarFornecedorUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CadastrarFornecedorSaida>> ExecutarAsync(
            CadastrarFornecedorEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CadastrarFornecedorUseCase),
                ["Nome"] = dados.Nome,
                ["Email"] = dados.Email
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cadastro de fornecedor."));

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
                resultadoDocumento,
                resultadoNome,
                resultadoEmail,
                resultadoEndereco);

            if (validacaoCampos.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para cadastro de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarFornecedorSaida>.Falha(validacaoCampos.Erros!);
            }

            var stopwatchExisteDocumento = Stopwatch.StartNew();

            var jaExiste = await _unitOfWork.FornecedoresRepository.ExistePorDocumentoAsync(
                resultadoDocumento.Instancia,
                cancellationToken);

            stopwatchExisteDocumento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de fornecedor por documento concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ExistePorDocumentoAsync",
                    ["DuracaoMs"] = stopwatchExisteDocumento.ElapsedMilliseconds
                }));

            if (jaExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência de fornecedor por documento.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["Erros"] = jaExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarFornecedorSaida>.Falha(jaExiste.Erros!);
            }

            if (jaExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de cadastro de fornecedor com documento já existente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarFornecedorSaida>.Falha("FORNECEDOR_JA_CADASTRADO");
            }

            var stopwatchCriarAgregado = Stopwatch.StartNew();

            var resultadoFornecedor = Fornecedor.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                resultadoEndereco.Instancia);

            stopwatchCriarAgregado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Criação do agregado Fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "Fornecedor.Criar",
                    ["DuracaoMs"] = stopwatchCriarAgregado.ElapsedMilliseconds
                }));

            if (resultadoFornecedor.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao criar agregado Fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["Erros"] = resultadoFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarFornecedorSaida>.Falha(resultadoFornecedor.Erros!);
            }

            var fornecedor = resultadoFornecedor.Instancia;

            var stopwatchAdicionar = Stopwatch.StartNew();

            await _unitOfWork.FornecedoresRepository.AdicionarAsync(
                fornecedor,
                cancellationToken);

            stopwatchAdicionar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Adição de fornecedor no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AdicionarAsync",
                    ["DuracaoMs"] = stopwatchAdicionar.ElapsedMilliseconds
                }));

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSaveChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final do cadastro de fornecedor concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSaveChanges.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir cadastro de fornecedor.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = fornecedor.Id.Valor,
                        ["Documento"] = fornecedor.Documento.Formatado,
                        ["Erros"] = resultadoSaveChanges.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarFornecedorSaida>.Falha(resultadoSaveChanges.Erros!);
            }

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Fornecedor cadastrado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["FornecedorId"] = fornecedor.Id.Valor,
                    ["Ativo"] = fornecedor.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CadastrarFornecedorSaida>.Sucesso(
                new CadastrarFornecedorSaida(
                    Id: fornecedor.Id.Valor,
                    Documento: fornecedor.Documento.Formatado,
                    Nome: fornecedor.Nome.Valor,
                    Email: fornecedor.Email.Valor,
                    Ativo: fornecedor.Ativo
                ));
        }
    }
}