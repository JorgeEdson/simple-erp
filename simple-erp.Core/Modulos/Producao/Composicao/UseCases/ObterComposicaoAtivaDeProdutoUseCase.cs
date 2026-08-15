using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IObterComposicaoAtivaDeProdutoUseCase
        : IUseCase<ObterComposicaoAtivaDeProdutoEntrada, ObterComposicaoAtivaDeProdutoSaida>
    {
    }

    public record ObterComposicaoAtivaDeProdutoEntrada(Guid IdProdutoFabricado) : IRequisicao<ObterComposicaoAtivaDeProdutoSaida>;

    public record ComposicaoAtivaItemSaida(
        Guid IdInsumo,
        decimal QuantidadePorUnidade);

    public record ObterComposicaoAtivaDeProdutoSaida(
        Guid IdProdutoFabricado,
        bool PossuiReceitaAtiva,
        Guid? IdComposicao,
        int? Versao,
        IReadOnlyCollection<ComposicaoAtivaItemSaida> Itens);

    public sealed class ObterComposicaoAtivaDeProdutoUseCase : IObterComposicaoAtivaDeProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterComposicaoAtivaDeProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterComposicaoAtivaDeProdutoSaida>> ExecutarAsync(ObterComposicaoAtivaDeProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterComposicaoAtivaDeProdutoUseCase),
                ["IdProdutoFabricado"] = dados.IdProdutoFabricado
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando consulta da composição ativa do produto."));

            #endregion

            #region Validação do identificador

            if (dados.IdProdutoFabricado == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterComposicaoAtivaDeProdutoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var existeAtiva = await _unitOfWork.ComposicoesDeProdutoRepository
                .ExisteAtivaPorProdutoAsync(dados.IdProdutoFabricado, cancellationToken);

            if (existeAtiva.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterComposicaoAtivaDeProdutoSaida>.Falha(existeAtiva.Erros!);
            }

            if (!existeAtiva.Instancia)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogInformation(new RegistroDeLog(
                    Mensagem: "Produto não possui receita ativa.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterComposicaoAtivaDeProdutoSaida>.Sucesso(
                    new ObterComposicaoAtivaDeProdutoSaida(
                        IdProdutoFabricado: dados.IdProdutoFabricado,
                        PossuiReceitaAtiva: false,
                        IdComposicao: null,
                        Versao: null,
                        Itens: new List<ComposicaoAtivaItemSaida>()));
            }

            var resultadoAtiva = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterAtivaPorProdutoAsync(dados.IdProdutoFabricado, cancellationToken);

            if (resultadoAtiva.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ObterComposicaoAtivaDeProdutoSaida>.Falha(resultadoAtiva.Erros!);
            }

            var composicao = resultadoAtiva.Instancia!;

            #endregion

            #region Mapeamento da saída

            var itens = composicao.Itens
                .Select(item => new ComposicaoAtivaItemSaida(
                    IdInsumo: item.IdInsumo,
                    QuantidadePorUnidade: item.QuantidadePorUnidade))
                .ToList();

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Consulta da composição ativa concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ComposicaoId"] = composicao.Id,
                    ["Versao"] = composicao.Versao,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterComposicaoAtivaDeProdutoSaida>.Sucesso(
                new ObterComposicaoAtivaDeProdutoSaida(
                    IdProdutoFabricado: dados.IdProdutoFabricado,
                    PossuiReceitaAtiva: true,
                    IdComposicao: composicao.Id,
                    Versao: composicao.Versao,
                    Itens: itens));

            #endregion
        }
    }
}
