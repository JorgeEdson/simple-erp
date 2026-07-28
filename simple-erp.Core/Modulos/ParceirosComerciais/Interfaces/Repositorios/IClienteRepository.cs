using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Especificacoes;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios
{
    public interface IClienteRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default);

        Task<Resultado<Cliente>> ObterPorIdAsync(Id id, CancellationToken cancellationToken = default);
        Task<Resultado<Cliente?>> ObterPorDocumentoAsync(Documento documento, CancellationToken cancellationToken = default);

        Task<Resultado<bool>> ExistePorIdAsync(Id id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indica se existe algum cliente que satisfaça a especificação. Substitui os antigos
        /// ExistePorDocumento/ExisteOutroPorDocumento: a unicidade no cadastro é
        /// <c>ParceiroComDocumentoSpecification</c>; na edição, essa spec composta com
        /// <c>ParceiroDiferenteDeSpecification</c> via <c>And</c>.
        /// </summary>
        Task<Resultado<bool>> ExisteAsync(ISpecification<Cliente> especificacao, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<Cliente>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarClientesFiltros? filtro = null,
            CancellationToken cancellationToken = default);
    }
}
