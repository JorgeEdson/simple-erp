using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.Producao;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Producao.Composicao.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do agregado Composição de Produto (a "receita"). O endereçamento é
    /// misto de propósito, seguindo o que o domínio usa como chave: definir, listar
    /// versões e obter a ativa são operações sobre o <em>produto</em>, então ficam
    /// aninhadas em <c>/api/produtos/{id}/composicoes</c>; ativar e inativar agem sobre
    /// uma <em>versão</em> específica, que tem id próprio, e ficam em
    /// <c>/api/composicoes/{id}</c>.
    ///
    /// Por isso não há <c>[Route]</c> no controller — cada ação declara o caminho
    /// completo.
    /// </summary>
    public sealed class ComposicoesDeProdutoController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public ComposicoesDeProdutoController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <remarks>
        /// Responde 200, não 201: o módulo não tem um "obter composição por id", então
        /// não há <c>Location</c> honesto para apontar. A versão criada vem no corpo e
        /// aparece na listagem de versões do produto.
        /// </remarks>
        [HttpPost("api/produtos/{idProdutoFabricado:long}/composicoes")]
        [ProducesResponseType(typeof(DefinirComposicaoDeProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Definir(
            long idProdutoFabricado,
            [FromBody] DefinirComposicaoDeProdutoRequest requisicao,
            CancellationToken cancellationToken)
        {
            // Lista ausente vira lista vazia: "receita sem insumo" é regra do domínio,
            // e é ele quem deve recusá-la com o código dele.
            var itens = (requisicao.Itens ?? Array.Empty<ItemDeComposicaoRequest>())
                .Select(item => new ItemDeComposicaoEntrada(
                    item.IdInsumo,
                    item.QuantidadePorUnidade))
                .ToArray();

            var entrada = new DefinirComposicaoDeProdutoEntrada(idProdutoFabricado, itens);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpGet("api/produtos/{idProdutoFabricado:long}/composicoes")]
        [ProducesResponseType(typeof(ListarVersoesDeComposicaoPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListarVersoes(
            long idProdutoFabricado,
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] bool? apenasAtivas = null)
        {
            var entrada = new ListarVersoesDeComposicaoPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                idProdutoFabricado,
                apenasAtivas);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Produto sem receita ativa não é 404: o use case devolve 200 com
        /// <c>PossuiReceitaAtiva = false</c>. "Ainda não tem receita" é uma resposta
        /// válida sobre o produto, não ausência do recurso — mesmo critério do saldo
        /// de estoque.
        /// </remarks>
        [HttpGet("api/produtos/{idProdutoFabricado:long}/composicoes/ativa")]
        [ProducesResponseType(typeof(ObterComposicaoAtivaDeProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObterAtiva(
            long idProdutoFabricado,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterComposicaoAtivaDeProdutoEntrada(idProdutoFabricado), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Ativar uma versão desativa a anterior do mesmo produto, mas não aqui: quem
        /// faz isso é o <c>ManipuladorUnicidadeDeReceitaAtiva</c>, reagindo ao evento
        /// <c>ComposicaoDeProdutoAtivada</c>. Como o despacho é pelo outbox, pode haver
        /// uma janela em que a leitura ainda enxergue a versão antiga como ativa.
        /// </remarks>
        [HttpPost("api/composicoes/{id:long}/ativar")]
        [ProducesResponseType(typeof(AtivarComposicaoDeProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Ativar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new AtivarComposicaoDeProdutoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("api/composicoes/{id:long}/inativar")]
        [ProducesResponseType(typeof(InativarComposicaoDeProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Inativar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new InativarComposicaoDeProdutoEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
