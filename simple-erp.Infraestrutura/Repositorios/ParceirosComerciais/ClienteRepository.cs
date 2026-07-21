using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.ParceirosComerciais
{
    public sealed class ClienteRepository : ParceiroComercialRepositoryBase<Cliente>, IClienteRepository
    {
        public ClienteRepository(SimpleErpDbContext contexto) : base(contexto)
        {
        }

        protected override string TabelaComSchema => "parceiros.clientes";

        public Task<Resultado<bool>> AdicionarAsync(
            Cliente cliente, CancellationToken cancellationToken = default) =>
            AdicionarInternoAsync(cliente, cancellationToken);

        public Task<Resultado<bool>> AtualizarAsync(
            Cliente cliente, CancellationToken cancellationToken = default) =>
            AtualizarInternoAsync(cliente, cancellationToken);

        public Task<Resultado<Cliente>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default) =>
            ObterPorIdInternoAsync(id, cancellationToken);

        public Task<Resultado<Cliente?>> ObterPorDocumentoAsync(
            Documento documento, CancellationToken cancellationToken = default) =>
            ObterPorDocumentoInternoAsync(documento, cancellationToken);

        public Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default) =>
            ExistePorIdInternoAsync(id, cancellationToken);

        public Task<Resultado<bool>> ExistePorDocumentoAsync(
            Documento documento, CancellationToken cancellationToken = default) =>
            ExistePorDocumentoInternoAsync(documento, cancellationToken);

        public Task<Resultado<bool>> ExisteOutroPorDocumentoAsync(
            Id idCliente, Documento documento, CancellationToken cancellationToken = default) =>
            ExisteOutroPorDocumentoInternoAsync(idCliente, documento, cancellationToken);

        public Task<Resultado<ResultadoPaginado<Cliente>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarClientesFiltros? filtro = null,
            CancellationToken cancellationToken = default) =>
            ListarPaginadoInternoAsync(
                numeroPagina,
                tamanhoPagina,
                filtro?.Nome,
                filtro?.Documento,
                filtro?.Email,
                filtro?.Ativo,
                filtro?.Cidade,
                filtro?.Estado,
                cancellationToken);
    }
}
