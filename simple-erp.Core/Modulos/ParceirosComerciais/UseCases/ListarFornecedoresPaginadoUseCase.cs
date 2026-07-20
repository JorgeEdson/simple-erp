using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IListarFornecedoresPaginadoUseCase
       : IUseCase<ListarFornecedoresPaginadoEntrada, ListarFornecedoresPaginadoSaida>
    {
    }

    public sealed record ListarFornecedoresPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        string? Nome = null,
        string? Documento = null,
        string? Email = null,
        bool? Ativo = null,
        string? Cidade = null,
        string? Estado = null);

    public sealed record ListarFornecedoresPaginadoSaida(
        int NumeroPagina,
        int TamanhoPagina,
        int TotalRegistros,
        int TotalPaginas,
        IReadOnlyCollection<ListarFornecedoresPaginadoItemSaida> Itens);

    public sealed record ListarFornecedoresPaginadoItemSaida(
        long Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        string Cidade,
        string Estado);

    public sealed record ListarFornecedoresFiltros(
        string? Nome = null,
        string? Documento = null,
        string? Email = null,
        bool? Ativo = null,
        string? Cidade = null,
        string? Estado = null);

    public sealed class ListarFornecedoresPaginadoUseCase : IListarFornecedoresPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarFornecedoresPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarFornecedoresPaginadoSaida>> ExecutarAsync(ListarFornecedoresPaginadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarFornecedoresPaginadoUseCase),
                ["NumeroPagina"] = dados.NumeroPagina,
                ["TamanhoPagina"] = dados.TamanhoPagina,
                ["Nome"] = dados.Nome,
                ["Documento"] = dados.Documento,
                ["Email"] = dados.Email,
                ["Ativo"] = dados.Ativo,
                ["Cidade"] = dados.Cidade,
                ["Estado"] = dados.Estado
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando listagem paginada de fornecedores."));

            #endregion

            #region Validação dos parâmetros

            var erros = new List<string>();

            if (dados.NumeroPagina <= 0)
                erros.Add("NUMERO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina <= 0)
                erros.Add("TAMANHO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina > 100)
                erros.Add("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            if (erros.Any())
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos parâmetros da listagem paginada de fornecedores.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarFornecedoresPaginadoSaida>.Falha(erros);
            }

            #endregion

            #region Consulta paginada

            var filtros = new ListarFornecedoresFiltros(
                Nome: dados.Nome,
                Documento: dados.Documento,
                Email: dados.Email,
                Ativo: dados.Ativo,
                Cidade: dados.Cidade,
                Estado: dados.Estado);

            var stopwatchListarPaginado = Stopwatch.StartNew();

            var resultadoPaginado = await _unitOfWork.FornecedoresRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtros,
                cancellationToken);

            stopwatchListarPaginado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta paginada de fornecedores no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ListarPaginadoAsync",
                    ["DuracaoMs"] = stopwatchListarPaginado.ElapsedMilliseconds
                }));

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar fornecedores paginados no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarFornecedoresPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            #endregion

            #region Mapeamento da saída

            var pagina = resultadoPaginado.Instancia;

            var stopwatchMapeamento = Stopwatch.StartNew();

            var itens = pagina.Itens
                .Select(fornecedor => new ListarFornecedoresPaginadoItemSaida(
                    Id: fornecedor.Id.Valor,
                    Nome: fornecedor.Nome.Valor,
                    Documento: fornecedor.Documento.Valor,
                    Email: fornecedor.Email.Valor,
                    Ativo: fornecedor.Ativo,
                    Cidade: fornecedor.Endereco.Cidade,
                    Estado: fornecedor.Endereco.Estado))
                .ToList();

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento dos itens da listagem paginada de fornecedores concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de fornecedores concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["NumeroPagina"] = pagina.NumeroPagina,
                    ["TamanhoPagina"] = pagina.TamanhoPagina,
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["TotalPaginas"] = pagina.TotalPaginas,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarFornecedoresPaginadoSaida>.Sucesso(
                new ListarFornecedoresPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));

            #endregion
        }
    }
}
