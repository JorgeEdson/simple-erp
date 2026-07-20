using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.Composicao.UseCases
{
    public interface IDefinirComposicaoDeProdutoUseCase
        : IUseCase<DefinirComposicaoDeProdutoEntrada, DefinirComposicaoDeProdutoSaida>
    {
    }

    public record ItemDeComposicaoEntrada(
        long IdInsumo,
        decimal QuantidadePorUnidade);

    public record DefinirComposicaoDeProdutoEntrada(
        long IdProdutoFabricado,
        IReadOnlyCollection<ItemDeComposicaoEntrada> Itens);

    public record ItemDeComposicaoSaida(
        long IdInsumo,
        decimal QuantidadePorUnidade);

    public record DefinirComposicaoDeProdutoSaida(
        long Id,
        long IdProdutoFabricado,
        int Versao,
        bool Ativa,
        IReadOnlyCollection<ItemDeComposicaoSaida> Itens);

    public sealed class DefinirComposicaoDeProdutoUseCase : IDefinirComposicaoDeProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public DefinirComposicaoDeProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<DefinirComposicaoDeProdutoSaida>> ExecutarAsync(DefinirComposicaoDeProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(DefinirComposicaoDeProdutoUseCase),
                ["IdProdutoFabricado"] = dados.IdProdutoFabricado,
                ["QuantidadeItens"] = dados.Itens?.Count ?? 0
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando definição de composição de produto."));

            #endregion

            #region Validação da entrada

            var resultadoIdProduto = Id.TentarCriar(dados.IdProdutoFabricado);

            if (resultadoIdProduto.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do produto fabricado para composição.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoIdProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoIdProduto.Erros!);
            }

            var resultadoItens = ConverterItens(dados.Itens);

            if (resultadoItens.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos itens da composição.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoItens.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoItens.Erros!);
            }

            var itens = resultadoItens.Instancia;

            #endregion

            #region Validação de pré-condições

            var resultadoFabricado = await ValidarProdutoFabricadoAsync(
                resultadoIdProduto.Instancia, cancellationToken);

            if (resultadoFabricado.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoFabricado.Erros!);
            }

            var resultadoInsumos = await ValidarInsumosSaoMateriaPrimaAsync(itens, cancellationToken);

            if (resultadoInsumos.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoInsumos.Erros!);
            }

            #endregion

            #region Execução das regras de negócio

                #region Criação da versão de composição

                var resultadoVersao = await _unitOfWork.ComposicoesDeProdutoRepository
                    .ObterProximaVersaoPorProdutoAsync(resultadoIdProduto.Instancia, cancellationToken);

                if (resultadoVersao.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao obter a próxima versão da composição.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoVersao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoVersao.Erros!);
                }

                var resultadoComposicao = ComposicaoDeProduto.Criar(
                    resultadoIdProduto.Instancia,
                    resultadoVersao.Instancia,
                    itens);

                if (resultadoComposicao.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao criar o agregado ComposicaoDeProduto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoComposicao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoComposicao.Erros!);
                }

                var composicao = resultadoComposicao.Instancia;

                #endregion

            #endregion

            #region Persistência

            await _unitOfWork.ComposicoesDeProdutoRepository.AdicionarAsync(composicao, cancellationToken);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir a composição de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<DefinirComposicaoDeProdutoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Composição de produto definida com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ComposicaoId"] = composicao.Id.Valor,
                    ["Versao"] = composicao.Versao,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<DefinirComposicaoDeProdutoSaida>.Sucesso(Mapear(composicao));

            #endregion
        }

        private static Resultado<List<ItemDeComposicao>> ConverterItens(
            IReadOnlyCollection<ItemDeComposicaoEntrada>? itensEntrada)
        {
            if (itensEntrada is null || itensEntrada.Count == 0)
                return Resultado<List<ItemDeComposicao>>.Falha("COMPOSICAO_SEM_ITENS");

            var itens = new List<ItemDeComposicao>();
            var erros = new List<string>();

            foreach (var itemEntrada in itensEntrada)
            {
                var resultadoIdInsumo = Id.TentarCriar(itemEntrada.IdInsumo);
                var resultadoQuantidade = Quantidade.TentarCriar(itemEntrada.QuantidadePorUnidade);

                var validacao = Resultado.Combinar(resultadoIdInsumo, resultadoQuantidade);

                if (validacao.EhFalha)
                {
                    erros.AddRange(validacao.Erros!);
                    continue;
                }

                var resultadoItem = ItemDeComposicao.TentarCriar(
                    resultadoIdInsumo.Instancia,
                    resultadoQuantidade.Instancia);

                if (resultadoItem.EhFalha)
                {
                    erros.AddRange(resultadoItem.Erros!);
                    continue;
                }

                itens.Add(resultadoItem.Instancia);
            }

            if (erros.Count > 0)
                return Resultado<List<ItemDeComposicao>>.Falha(erros);

            return Resultado<List<ItemDeComposicao>>.Sucesso(itens);
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

        private async Task<Resultado<bool>> ValidarInsumosSaoMateriaPrimaAsync(
            IReadOnlyCollection<ItemDeComposicao> itens,
            CancellationToken cancellationToken)
        {
            var idsInsumos = itens.Select(item => item.IdInsumo).Distinct().ToList();

            foreach (var idInsumo in idsInsumos)
            {
                var resultadoId = Id.TentarCriar(idInsumo);

                if (resultadoId.EhFalha)
                    return Resultado<bool>.Falha(resultadoId.Erros!);

                var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                    resultadoId.Instancia, cancellationToken);

                if (resultadoProduto.EhFalha)
                    return Resultado<bool>.Falha(resultadoProduto.Erros!);

                var insumo = resultadoProduto.Instancia;

                if (insumo is null)
                    return Resultado<bool>.Falha("INSUMO_NAO_ENCONTRADO");

                if (!insumo.EhMateriaPrima)
                    return Resultado<bool>.Falha("INSUMO_NAO_E_MATERIA_PRIMA");
            }

            return Resultado<bool>.Sucesso(true);
        }

        private static DefinirComposicaoDeProdutoSaida Mapear(ComposicaoDeProduto composicao)
        {
            var itens = composicao.Itens
                .Select(item => new ItemDeComposicaoSaida(
                    IdInsumo: item.IdInsumo,
                    QuantidadePorUnidade: item.QuantidadePorUnidade))
                .ToList();

            return new DefinirComposicaoDeProdutoSaida(
                Id: composicao.Id.Valor,
                IdProdutoFabricado: composicao.IdProdutoFabricado.Valor,
                Versao: composicao.Versao,
                Ativa: composicao.Ativa,
                Itens: itens);
        }
    }
}
