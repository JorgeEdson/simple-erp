using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IObterClientePorIdUseCase : IUseCase<ObterClientePorIdEntrada, ObterClientePorIdSaida>
    {
    }

    public sealed record ObterClientePorIdEntrada(Guid Id) : IRequisicao<ObterClientePorIdSaida>;

    public sealed record ObterClientePorIdSaida(
        Guid Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc,
        ObterClientePorIdEnderecoSaida Endereco);

    public sealed record ObterClientePorIdEnderecoSaida(
        string Rua,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string? Complemento,
        string Pais);

    public sealed class ObterClientePorIdUseCase : IObterClientePorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterClientePorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterClientePorIdSaida>> ExecutarAsync(ObterClientePorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterClientePorIdUseCase),
                ["ClienteId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando obtenção de cliente por id."));

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

                return Resultado<ObterClientePorIdSaida>.Falha("ID_INVALIDO");
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
                    Mensagem: "Falha ao obter cliente por id no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = dados.Id,
                        ["Erros"] = resultadoCliente.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterClientePorIdSaida>.Falha(resultadoCliente.Erros!);
            }

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de obtenção de cliente não encontrado por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ClienteId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterClientePorIdSaida>.Falha("CLIENTE_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var stopwatchMapeamento = Stopwatch.StartNew();

            var saida = new ObterClientePorIdSaida(
                Id: cliente.Id,
                Nome: cliente.Nome.Valor,
                Documento: cliente.Documento.Valor,
                Email: cliente.Email.Valor,
                Ativo: cliente.Ativo,
                DataCriacaoUtc: cliente.DataCriacaoUtc,
                DataAtualizacaoUtc: cliente.DataAtualizacaoUtc,
                Endereco: new ObterClientePorIdEnderecoSaida(
                    Rua: cliente.Endereco.Rua,
                    Numero: cliente.Endereco.Numero,
                    Bairro: cliente.Endereco.Bairro,
                    Cidade: cliente.Endereco.Cidade,
                    Estado: cliente.Endereco.Estado,
                    Cep: cliente.Endereco.Cep,
                    Complemento: cliente.Endereco.Complemento,
                    Pais: cliente.Endereco.Pais));

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento da saída de obtenção de cliente por id concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoMapeamento"] = "ObterClientePorIdSaida",
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Cliente obtido por id com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ClienteId"] = cliente.Id,
                    ["Ativo"] = cliente.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterClientePorIdSaida>.Sucesso(saida);

            #endregion
        }
    }
}
