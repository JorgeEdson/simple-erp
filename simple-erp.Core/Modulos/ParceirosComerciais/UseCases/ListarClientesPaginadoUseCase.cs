using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IListarClientesPaginadoUseCase
       : IUseCase<ListarClientesPaginadoEntrada, ListarClientesPaginadoSaida>
    {
    }

    public sealed record ListarClientesPaginadoEntrada(
        int NumeroPagina,
        int TamanhoPagina,
        string? Nome = null,
        string? Documento = null,
        string? Email = null,
        bool? Ativo = null,
        string? Cidade = null,
        string? Estado = null,
        DateTime? DataCriacaoInicialUtc = null,
        DateTime? DataCriacaoFinalUtc = null);

    public sealed record ListarClientesPaginadoSaida(
       int NumeroPagina,
       int TamanhoPagina,
       int TotalRegistros,
       int TotalPaginas,
       IReadOnlyCollection<ListarClientesPaginadoItemSaida> Itens);

    public sealed record ListarClientesPaginadoItemSaida(
        long Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        string Cidade,
        string Estado);

    public sealed record ListarClientesFiltros(
        string? Nome = null,
        string? Documento = null,
        string? Email = null,
        bool? Ativo = null,
        string? Cidade = null,
        string? Estado = null);

    public sealed class ListarClientesPaginadoUseCase : IListarClientesPaginadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ListarClientesPaginadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ListarClientesPaginadoSaida>> ExecutarAsync(
            ListarClientesPaginadoEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ListarClientesPaginadoUseCase),
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
                Mensagem: "Iniciando listagem paginada de clientes."));

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
                    Mensagem: "Falha na validação dos parâmetros da listagem paginada de clientes.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = erros.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarClientesPaginadoSaida>.Falha(erros);
            }

            var filtro = new ListarClientesFiltros(
                Nome: dados.Nome,
                Documento: dados.Documento,
                Email: dados.Email,
                Ativo: dados.Ativo,
                Cidade: dados.Cidade,
                Estado: dados.Estado);

            var stopwatchListarPaginado = Stopwatch.StartNew();

            var resultadoPaginado = await _unitOfWork.ClientesRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtro,
                cancellationToken);

            stopwatchListarPaginado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta paginada de clientes no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ListarPaginadoAsync",
                    ["DuracaoMs"] = stopwatchListarPaginado.ElapsedMilliseconds
                }));

            if (resultadoPaginado.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao listar clientes paginados no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoPaginado.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ListarClientesPaginadoSaida>.Falha(resultadoPaginado.Erros!);
            }

            var pagina = resultadoPaginado.Instancia;

            var stopwatchMapeamento = Stopwatch.StartNew();

            var itens = pagina.Itens
                .Select(cliente => new ListarClientesPaginadoItemSaida(
                    Id: cliente.Id.Valor,
                    Nome: cliente.Nome.Valor,
                    Documento: cliente.Documento.Valor,
                    Email: cliente.Email.Valor,
                    Ativo: cliente.Ativo,
                    Cidade: cliente.Endereco.Cidade,
                    Estado: cliente.Endereco.Estado))
                .ToList();

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento dos itens da listagem paginada de clientes concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Listagem paginada de clientes concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["NumeroPagina"] = pagina.NumeroPagina,
                    ["TamanhoPagina"] = pagina.TamanhoPagina,
                    ["TotalRegistros"] = pagina.TotalRegistros,
                    ["TotalPaginas"] = pagina.TotalPaginas,
                    ["QuantidadeItens"] = itens.Count,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ListarClientesPaginadoSaida>.Sucesso(
                new ListarClientesPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));
        }
    }
}
