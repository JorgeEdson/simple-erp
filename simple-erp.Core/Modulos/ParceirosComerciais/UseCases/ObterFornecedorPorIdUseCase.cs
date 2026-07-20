using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IObterFornecedorPorIdUseCase
       : IUseCase<ObterFornecedorPorIdEntrada, ObterFornecedorPorIdSaida>
    {
    }

    public record ObterFornecedorPorIdEntrada(long Id);

    public record ObterFornecedorPorIdSaida(
        long Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc,
        ObterFornecedorPorIdEnderecoSaida Endereco
    );

    public record ObterFornecedorPorIdEnderecoSaida(
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string Pais
    );

    public sealed class ObterFornecedorPorIdUseCase : IObterFornecedorPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterFornecedorPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterFornecedorPorIdSaida>> ExecutarAsync(ObterFornecedorPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterFornecedorPorIdUseCase),
                ["FornecedorId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando obtenção de fornecedor por id."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para obtenção de fornecedor por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterFornecedorPorIdSaida>.Falha(resultadoId.Erros!);
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
                    Mensagem: "Falha ao obter fornecedor por id no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoFornecedor.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterFornecedorPorIdSaida>.Falha(resultadoFornecedor.Erros!);
            }

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de obtenção de fornecedor não encontrado por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["FornecedorId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterFornecedorPorIdSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var stopwatchMapeamento = Stopwatch.StartNew();

            var saida = new ObterFornecedorPorIdSaida(
                Id: fornecedor.Id.Valor,
                Nome: fornecedor.Nome.Valor,
                Documento: fornecedor.Documento.Valor,
                Email: fornecedor.Email.Valor,
                Ativo: fornecedor.Ativo,
                DataCriacaoUtc: fornecedor.DataCriacaoUtc,
                DataAtualizacaoUtc: fornecedor.DataAtualizacaoUtc,
                Endereco: new ObterFornecedorPorIdEnderecoSaida(
                    Rua: fornecedor.Endereco.Rua,
                    Numero: fornecedor.Endereco.Numero,
                    Complemento: fornecedor.Endereco.Complemento,
                    Bairro: fornecedor.Endereco.Bairro,
                    Cidade: fornecedor.Endereco.Cidade,
                    Estado: fornecedor.Endereco.Estado,
                    Cep: fornecedor.Endereco.Cep,
                    Pais: fornecedor.Endereco.Pais));

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento da saída de obtenção de fornecedor por id concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoMapeamento"] = "ObterFornecedorPorIdSaida",
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Fornecedor obtido por id com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["FornecedorId"] = fornecedor.Id.Valor,
                    ["Ativo"] = fornecedor.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterFornecedorPorIdSaida>.Sucesso(saida);

            #endregion
        }
    }
}
