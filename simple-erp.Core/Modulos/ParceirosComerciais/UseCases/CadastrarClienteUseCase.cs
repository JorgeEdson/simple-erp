using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Diagnostics;

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
        private readonly ILogService _logService;

        public CadastrarClienteUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CadastrarClienteSaida>> ExecutarAsync(
            CadastrarClienteEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CadastrarClienteUseCase),
                ["Nome"] = dados.Nome,
                ["Email"] = dados.Email
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cadastro de cliente."));

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
                    Mensagem: "Falha na validação dos dados para cadastro de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarClienteSaida>.Falha(validacaoCampos.Erros!);
            }

            var stopwatchExisteDocumento = Stopwatch.StartNew();

            var jaExiste = await _unitOfWork.ClientesRepository.ExistePorDocumentoAsync(
                resultadoDocumento.Instancia,
                cancellationToken);

            stopwatchExisteDocumento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de cliente por documento concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ExistePorDocumentoAsync",
                    ["DuracaoMs"] = stopwatchExisteDocumento.ElapsedMilliseconds
                }));

            if (jaExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência de cliente por documento.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["Erros"] = jaExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarClienteSaida>.Falha(jaExiste.Erros!);
            }

            if (jaExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de cadastro de cliente com documento já existente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarClienteSaida>.Falha("CLIENTE_JA_CADASTRADO");
            }

            var stopwatchCriarAgregado = Stopwatch.StartNew();

            var resultadoCliente = Cliente.Criar(
                resultadoNome.Instancia,
                resultadoDocumento.Instancia,
                resultadoEmail.Instancia,
                resultadoEndereco.Instancia);

            stopwatchCriarAgregado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Criação do agregado Cliente concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "Cliente.Criar",
                    ["DuracaoMs"] = stopwatchCriarAgregado.ElapsedMilliseconds
                }));

            if (resultadoCliente.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao criar agregado Cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Documento"] = resultadoDocumento.Instancia.Formatado,
                        ["Erros"] = resultadoCliente.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarClienteSaida>.Falha(resultadoCliente.Erros!);
            }

            var cliente = resultadoCliente.Instancia;

            var stopwatchAdicionar = Stopwatch.StartNew();

            await _unitOfWork.ClientesRepository.AdicionarAsync(
                cliente,
                cancellationToken);

            stopwatchAdicionar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Adição de cliente no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AdicionarAsync",
                    ["DuracaoMs"] = stopwatchAdicionar.ElapsedMilliseconds
                }));

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSaveChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final do cadastro de cliente concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSaveChanges.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir cadastro de cliente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = cliente.Id.Valor,
                        ["Documento"] = cliente.Documento.Formatado,
                        ["Erros"] = resultadoSaveChanges.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarClienteSaida>.Falha(resultadoSaveChanges.Erros!);
            }

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Cliente cadastrado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ClienteId"] = cliente.Id.Valor,
                    ["Ativo"] = cliente.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CadastrarClienteSaida>.Sucesso(
                new CadastrarClienteSaida(
                    Id: cliente.Id.Valor,
                    Documento: cliente.Documento.Formatado,
                    Nome: cliente.Nome.Valor,
                    Email: cliente.Email.Valor,
                    Ativo: cliente.Ativo));
        }
    }
}
