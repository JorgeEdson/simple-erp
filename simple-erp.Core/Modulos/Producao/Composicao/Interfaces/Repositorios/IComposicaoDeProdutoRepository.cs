using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Dominio;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.UseCases;

namespace simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios
{
    public interface IComposicaoDeProdutoRepository : IRepositorio
    {
        Task<Resultado<bool>> AdicionarAsync(ComposicaoDeProduto composicao, CancellationToken cancellationToken = default);
        Task<Resultado<bool>> AtualizarAsync(ComposicaoDeProduto composicao, CancellationToken cancellationToken = default);

        /// <summary>Retorna a composição pelo id (contrato: Sucesso com instância, ou Falha quando não encontrada).</summary>
        Task<Resultado<ComposicaoDeProduto?>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Indica se o produto possui alguma versão de receita atualmente ativa.</summary>
        Task<Resultado<bool>> ExisteAtivaPorProdutoAsync(Guid idProdutoFabricado, CancellationToken cancellationToken = default);

        /// <summary>Retorna a versão ativa da receita do produto. Só deve ser chamado quando <see cref="ExisteAtivaPorProdutoAsync"/> for verdadeiro.</summary>
        Task<Resultado<ComposicaoDeProduto?>> ObterAtivaPorProdutoAsync(Guid idProdutoFabricado, CancellationToken cancellationToken = default);

        /// <summary>Retorna o próximo número de versão para a receita do produto (1 quando não houver nenhuma).</summary>
        Task<Resultado<int>> ObterProximaVersaoPorProdutoAsync(Guid idProdutoFabricado, CancellationToken cancellationToken = default);

        Task<Resultado<ResultadoPaginado<ComposicaoDeProduto>>> ListarPorProdutoPaginadoAsync(
            int numeroPagina,
            int tamanhoPagina,
            ListarVersoesDeComposicaoFiltros filtro,
            CancellationToken cancellationToken = default);
    }
}
