using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

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

        public ListarFornecedoresPaginadoUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<ListarFornecedoresPaginadoSaida>> ExecutarAsync(
            ListarFornecedoresPaginadoEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var erros = new List<string>();

            if (dados.NumeroPagina <= 0)
                erros.Add("NUMERO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina <= 0)
                erros.Add("TAMANHO_PAGINA_INVALIDO");

            if (dados.TamanhoPagina > 100)
                erros.Add("TAMANHO_PAGINA_MAXIMO_EXCEDIDO");

            if (erros.Any())
                return Resultado<ListarFornecedoresPaginadoSaida>.Falha(erros);

            var filtros = new ListarFornecedoresFiltros(
                Nome: dados.Nome,
                Documento: dados.Documento,
                Email: dados.Email,
                Ativo: dados.Ativo,
                Cidade: dados.Cidade,
                Estado: dados.Estado);

            var resultadoPaginado = await _unitOfWork.FornecedoresRepository.ListarPaginadoAsync(
                dados.NumeroPagina,
                dados.TamanhoPagina,
                filtros,
                cancellationToken);

            if (resultadoPaginado.EhFalha)
                return Resultado<ListarFornecedoresPaginadoSaida>.Falha(resultadoPaginado.Erros!);

            var pagina = resultadoPaginado.Instancia;

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

            return Resultado<ListarFornecedoresPaginadoSaida>.Sucesso(
                new ListarFornecedoresPaginadoSaida(
                    NumeroPagina: pagina.NumeroPagina,
                    TamanhoPagina: pagina.TamanhoPagina,
                    TotalRegistros: pagina.TotalRegistros,
                    TotalPaginas: pagina.TotalPaginas,
                    Itens: itens));
        }
    }
}
