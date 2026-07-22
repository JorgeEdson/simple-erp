using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface ICriarOrdemDeProducaoUseCase
        : IUseCase<CriarOrdemDeProducaoEntrada, CriarOrdemDeProducaoSaida>
    {
    }

    public record CriarOrdemDeProducaoEntrada(
        long IdProdutoFabricado,
        decimal QuantidadeAProduzir) : IRequisicao<CriarOrdemDeProducaoSaida>;

    public record NecessidadeDeMateriaPrimaSaida(
        long IdInsumo,
        decimal QuantidadeNecessaria);

    public record CriarOrdemDeProducaoSaida(
        long Id,
        long IdProdutoFabricado,
        long IdComposicao,
        decimal QuantidadeAProduzir,
        string Status,
        IReadOnlyCollection<NecessidadeDeMateriaPrimaSaida> Necessidades);

    public sealed class CriarOrdemDeProducaoUseCase : ICriarOrdemDeProducaoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CriarOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CriarOrdemDeProducaoSaida>> ExecutarAsync(CriarOrdemDeProducaoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CriarOrdemDeProducaoUseCase),
                ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                ["QuantidadeAProduzir"] = dados.QuantidadeAProduzir
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando criação de ordem de produção."));

            #endregion

            #region Validação da entrada

            var resultadoIdProduto = Id.TentarCriar(dados.IdProdutoFabricado);
            var resultadoQuantidade = Quantidade.TentarCriar(dados.QuantidadeAProduzir);

            var validacao = Resultado.Combinar(resultadoIdProduto, resultadoQuantidade);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para criação de ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarOrdemDeProducaoSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Validação de pré-condições

            var resultadoFabricado = await ValidarProdutoFabricadoAsync(
                resultadoIdProduto.Instancia, cancellationToken);

            if (resultadoFabricado.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoFabricado.Erros!);
            }

            var existeAtiva = await _unitOfWork.ComposicoesDeProdutoRepository
                .ExisteAtivaPorProdutoAsync(resultadoIdProduto.Instancia, cancellationToken);

            if (existeAtiva.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CriarOrdemDeProducaoSaida>.Falha(existeAtiva.Erros!);
            }

            if (!existeAtiva.Instancia)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de criar ordem de produção sem composição ativa.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarOrdemDeProducaoSaida>.Falha("COMPOSICAO_ATIVA_NAO_ENCONTRADA");
            }

            var resultadoComposicao = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterAtivaPorProdutoAsync(resultadoIdProduto.Instancia, cancellationToken);

            if (resultadoComposicao.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoComposicao.Erros!);
            }

            var composicao = resultadoComposicao.Instancia!;

            #endregion

            #region Execução das regras de negócio

                #region Cálculo das necessidades de matéria-prima

                var resultadoNecessidadesCalculadas = composicao.CalcularNecessidades(resultadoQuantidade.Instancia);

                if (resultadoNecessidadesCalculadas.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao calcular as necessidades de matéria-prima.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoNecessidadesCalculadas.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoNecessidadesCalculadas.Erros!);
                }

                var resultadoNecessidades = ConverterNecessidades(resultadoNecessidadesCalculadas.Instancia);

                if (resultadoNecessidades.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoNecessidades.Erros!);
                }

                #endregion

                #region Criação da ordem de produção

                var resultadoOrdem = OrdemDeProducao.Criar(
                    resultadoIdProduto.Instancia,
                    composicao.Id,
                    resultadoQuantidade.Instancia,
                    resultadoNecessidades.Instancia);

                if (resultadoOrdem.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao criar o agregado OrdemDeProducao.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoOrdem.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoOrdem.Erros!);
                }

                var ordem = resultadoOrdem.Instancia;

                #endregion

            #endregion

            #region Persistência

            await _unitOfWork.OrdensDeProducaoRepository.AdicionarAsync(ordem, cancellationToken);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir a ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<CriarOrdemDeProducaoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção criada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id.Valor,
                    ["IdComposicao"] = ordem.IdComposicao.Valor,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CriarOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));

            #endregion
        }

        private async Task<Resultado<bool>> ValidarProdutoFabricadoAsync(
            Id idProdutoFabricado,
            CancellationToken cancellationToken)
        {
            var resultado = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                idProdutoFabricado, cancellationToken);

            if (resultado.EhFalha)
                return Resultado<bool>.Falha(resultado.Erros!);

            var produto = resultado.Instancia;

            if (produto is null)
                return Resultado<bool>.Falha("PRODUTO_FABRICADO_NAO_ENCONTRADO");

            if (!produto.EhFabricado)
                return Resultado<bool>.Falha("PRODUTO_NAO_E_FABRICADO");

            return Resultado<bool>.Sucesso(true);
        }

        private static Resultado<List<NecessidadeDeMateriaPrima>> ConverterNecessidades(
            IReadOnlyCollection<NecessidadeCalculada> calculadas)
        {
            var necessidades = new List<NecessidadeDeMateriaPrima>();
            var erros = new List<string>();

            foreach (var calculada in calculadas)
            {
                var resultadoIdInsumo = Id.TentarCriar(calculada.IdInsumo);
                var resultadoQuantidade = Quantidade.TentarCriar(calculada.QuantidadeTotal);

                var validacao = Resultado.Combinar(resultadoIdInsumo, resultadoQuantidade);

                if (validacao.EhFalha)
                {
                    erros.AddRange(validacao.Erros!);
                    continue;
                }

                var resultadoNecessidade = NecessidadeDeMateriaPrima.TentarCriar(
                    resultadoIdInsumo.Instancia,
                    resultadoQuantidade.Instancia);

                if (resultadoNecessidade.EhFalha)
                {
                    erros.AddRange(resultadoNecessidade.Erros!);
                    continue;
                }

                necessidades.Add(resultadoNecessidade.Instancia);
            }

            if (erros.Count > 0)
                return Resultado<List<NecessidadeDeMateriaPrima>>.Falha(erros);

            return Resultado<List<NecessidadeDeMateriaPrima>>.Sucesso(necessidades);
        }

        private static CriarOrdemDeProducaoSaida Mapear(OrdemDeProducao ordem)
        {
            var necessidades = ordem.Necessidades
                .Select(necessidade => new NecessidadeDeMateriaPrimaSaida(
                    IdInsumo: necessidade.IdInsumo,
                    QuantidadeNecessaria: necessidade.QuantidadeNecessaria))
                .ToList();

            return new CriarOrdemDeProducaoSaida(
                Id: ordem.Id.Valor,
                IdProdutoFabricado: ordem.IdProdutoFabricado.Valor,
                IdComposicao: ordem.IdComposicao.Valor,
                QuantidadeAProduzir: ordem.QuantidadeAProduzir,
                Status: ordem.Status.ToString(),
                Necessidades: necessidades);
        }
    }
}
