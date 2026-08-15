using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IObterProdutoPorIdUseCase : IUseCase<ObterProdutoPorIdEntrada, ObterProdutoPorIdSaida>
    {
    }

    public sealed record ObterProdutoPorIdEntrada(Guid Id) : IRequisicao<ObterProdutoPorIdSaida>;

    public sealed record ObterProdutoPorIdSaida(
        Guid Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string Classificacao,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc);

    public sealed class ObterProdutoPorIdUseCase : IObterProdutoPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterProdutoPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterProdutoPorIdSaida>> ExecutarAsync(ObterProdutoPorIdEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterProdutoPorIdUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando obtenção de produto por id."));

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

                return Resultado<ObterProdutoPorIdSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterProduto = Stopwatch.StartNew();

            var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                dados.Id,
                cancellationToken);

            stopwatchObterProduto.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de produto por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObterProduto.ElapsedMilliseconds
                }));

            if (resultadoProduto.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter produto por id no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterProdutoPorIdSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de obtenção de produto não encontrado por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterProdutoPorIdSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Mapeamento da saída

            var stopwatchMapeamento = Stopwatch.StartNew();

            var saida = new ObterProdutoPorIdSaida(
                Id: produto.Id,
                Codigo: produto.Codigo.Valor,
                Descricao: produto.Descricao.Valor,
                UnidadeDeMedida: produto.UnidadeDeMedida.Valor,
                Classificacao: produto.Classificacao.ToString(),
                Ativo: produto.Ativo,
                DataCriacaoUtc: produto.DataCriacaoUtc,
                DataAtualizacaoUtc: produto.DataAtualizacaoUtc);

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento da saída de obtenção de produto por id concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoMapeamento"] = "ObterProdutoPorIdSaida",
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto obtido por id com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id,
                    ["Ativo"] = produto.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterProdutoPorIdSaida>.Sucesso(saida);

            #endregion
        }
    }
}
