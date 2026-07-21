using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Repositorios.ParceirosComerciais
{
    public sealed class FornecedorRepository
        : ParceiroComercialRepositoryBase<Fornecedor>, IFornecedorRepository
    {
        public FornecedorRepository(SimpleErpDbContext contexto)
            : base(contexto)
        {
        }

        protected override string TabelaComSchema => "parceiros.fornecedores";

        public Task<Resultado<bool>> AdicionarAsync(
            Fornecedor fornecedor, CancellationToken cancellationToken = default) =>
            AdicionarInternoAsync(fornecedor, cancellationToken);

        public Task<Resultado<bool>> AtualizarAsync(
            Fornecedor fornecedor, CancellationToken cancellationToken = default) =>
            AtualizarInternoAsync(fornecedor, cancellationToken);

        public Task<Resultado<Fornecedor>> ObterPorIdAsync(
            Id id, CancellationToken cancellationToken = default) =>
            ObterPorIdInternoAsync(id, cancellationToken);

        public Task<Resultado<Fornecedor?>> ObterPorDocumentoAsync(
            Documento documento, CancellationToken cancellationToken = default) =>
            ObterPorDocumentoInternoAsync(documento, cancellationToken);

        public Task<Resultado<bool>> ExistePorIdAsync(
            Id id, CancellationToken cancellationToken = default) =>
            ExistePorIdInternoAsync(id, cancellationToken);

        public Task<Resultado<bool>> ExistePorDocumentoAsync(
            Documento documento, CancellationToken cancellationToken = default) =>
            ExistePorDocumentoInternoAsync(documento, cancellationToken);

        public Task<Resultado<bool>> ExisteOutroPorDocumentoAsync(
            Id idFornecedor, Documento documento, CancellationToken cancellationToken = default) =>
            ExisteOutroPorDocumentoInternoAsync(idFornecedor, documento, cancellationToken);

        public Task<Resultado<ResultadoPaginado<Fornecedor>>> ListarPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarFornecedoresFiltros? filtro = null,
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
