using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IListarProdutosPaginadoUseCase
       : IUseCase<ListarProdutosPaginadoEntrada, ListarProdutosPaginadoSaida>
    {
    }

    public sealed record ListarProdutosPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        string? Codigo = null,
        string? Descricao = null,
        string? UnidadeDeMedida = null,
        string? Classificacao = null,
        bool? Ativo = null);

    public sealed record ListarProdutosPaginadoSaida(
       int NumeroPagina,
       int TamanhoPagina,
       int TotalRegistros,
       int TotalPaginas,
       IReadOnlyCollection<ListarProdutosPaginadoItemSaida> Itens);

    public sealed record ListarProdutosPaginadoItemSaida(
        long Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string Classificacao,
        bool Ativo);

    public sealed record ListarProdutosFiltros(
        string? Codigo = null,
        string? Descricao = null,
        string? UnidadeDeMedida = null,
        ClassificacaoProduto? Classificacao = null,
        bool? Ativo = null);

    public sealed class ListarProdutosPaginadoUseCase : IListarProdutosPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarProdutosPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarProdutosPaginadoSaida>> ExecutarAsync(ListarProdutosPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarProdutosPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["Codigo"] = dados.Codigo,
                ["Descricao"] = dados.Descricao,
                ["UnidadeDeMedida"] = dados.UnidadeDeMedida,
                ["Classificacao"] = dados.Classificacao,
                ["Ativo"] = dados.Ativo
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de produtos."));

            #endregion

            #region Validação dos parâmetros

            var erros = new List<string>();

            if (dados.NumeroPagina <= 0)
                erros.Add("NUMERO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina <= 0)
                erros.Add("TAMANHO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina > 100)
                erros.Add("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            ClassificacaoProduto? classificacaoFiltro = null;

            if (!string.IsNullOrWhiteSpace(dados.Classificacao))
            {
                if (Enum.TryParse<ClassificacaoProduto>(dados.Classificacao, true, out var classificacaoParseada))
                {
                    classificacaoFiltro = classificacaoParseada;
                }
                else
                {
                    erros.Add("CLASSIFICACAO_PRODUTO_INVALIDA");
                }
            }

            if (erros.Any())
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos parâmetros da listagem paginada de produtos.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarProdutosPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtro = new ListarProdutosFiltros(
                Codigo: dados.Codigo,
                Descricao: dados.Descricao,
                UnidadeDeMedida: dados.UnidadeDeMedida,
                Classificacao: classificacaoFiltro,
                Ativo: dados.Ativo);

            var stopwatchListarPaginado = Stopwatch.StartNew();

            var resultadoPaginado = await _unitOfWork.ProdutosRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtro,
                cancellationToken);

            stopwatchListarPaginado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta paginada de produtos no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ListarPaginadoAsync",
                    ["DuracaoMs"] = stopwatchListarPaginado.ElapsedMilliseconds
                }));

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar produtos paginados no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarProdutosPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var stopwatchMapeamento = Stopwatch.StartNew();

            var itens = pagina.Itens
                .Select(produto => new ListarProdutosPaginadoItemSaida(
                    Id: produto.Id.Valor,
                    Codigo: produto.Codigo.Valor,
                    Descricao: produto.Descricao.Valor,
                    UnidadeDeMedida: produto.UnidadeDeMedida.Valor,
                    Classificacao: produto.Classificacao.ToString(),
                    Ativo: produto.Ativo))
                .ToList();

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento dos itens da listagem paginada de produtos concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de produtos concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["NumeroPagina"] = pagina.NumeroPagina,
                    ["TamanhoPagina"] = pagina.TamanhoPagina,
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["TotalPaginas"] = pagina.TotalPaginas,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarProdutosPaginadoSaida>.Sucesso(
                new ListarProdutosPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
