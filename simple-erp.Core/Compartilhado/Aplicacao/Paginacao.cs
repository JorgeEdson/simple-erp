namespace simple_erp.Core.Compartilhado.DTOs
{
    public record EntradaPaginada(
    int Pagina,
    int TamanhoPagina
    );

    public record MetadadosDaPaginacao(
        int Pagina,
        int TamanhoPagina,
        int Total,
        int TotalPaginas
    );

    public record ResultadoPaginado<T>(
        IReadOnlyCollection<T> Dados,
        MetadadosDaPaginacao Metadados
    );
}
