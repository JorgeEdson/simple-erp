using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.Suprimentos;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do agregado Pedido de Compra (módulo Suprimentos). Ao contrário de
    /// Estoque e Financeiro, aqui todos os use cases são acionados por usuário —
    /// nenhum handler de evento os consome — então todos têm porta HTTP.
    ///
    /// O ciclo do pedido é EmEdicao → Aprovada → Concluida, com Cancelada como saída.
    /// Cada transição é um POST próprio em vez de um PATCH de status: quem chama pede
    /// a ação ("aprovar"), e é o agregado que decide se ela é válida no estado atual.
    /// </summary>
    [Route("api/pedidos-de-compra")]
    public sealed class PedidosDeCompraController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public PedidosDeCompraController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CriarPedidoDeCompraSaida), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar(
            [FromBody] CriarPedidoDeCompraRequest requisicao,
            CancellationToken cancellationToken)
        {
            // Lista ausente vira lista vazia, não exceção: "pedido sem item" é uma
            // regra do domínio, e é ele quem deve recusá-la com o código dele.
            var itens = (requisicao.Itens ?? Array.Empty<ItemDePedidoDeCompraRequest>())
                .Select(item => new ItemPedidoDeCompraEntrada(
                    item.IdProduto,
                    item.Quantidade,
                    item.CustoUnitario))
                .ToArray();

            var entrada = new CriarPedidoDeCompraEntrada(requisicao.IdFornecedor, itens);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);

            return Criado(resultado, RotaObterPorId, saida => new { id = saida.Id });
        }

        // Nome de rota único na aplicação (usado pelo CreatedAtRoute do POST).
        private const string RotaObterPorId = "ObterPedidoDeCompraPorId";

        [HttpGet("{id:long}", Name = RotaObterPorId)]
        [ProducesResponseType(typeof(ObterPedidoDeCompraPorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterPedidoDeCompraPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarPedidosDeCompraPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] long? idFornecedor = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null)
        {
            if (!TentarConverterEnum<StatusPedidoDeCompra>(
                    status, "STATUS_PEDIDO_DE_COMPRA_INVALIDO", out var statusConvertido, out var erroStatus))
                return BadRequest(erroStatus);

            var entrada = new ListarPedidosDeCompraPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                idFornecedor,
                statusConvertido,
                dataInicio,
                dataFim);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/itens")]
        [ProducesResponseType(typeof(AdicionarItemAoPedidoDeCompraSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdicionarItem(
            long id,
            [FromBody] AdicionarItemAoPedidoDeCompraRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new AdicionarItemAoPedidoDeCompraEntrada(
                id,
                requisicao.IdProduto,
                requisicao.Quantidade,
                requisicao.CustoUnitario);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// O item é endereçado pelo produto, não por um id próprio: o domínio trata o
        /// produto como chave do item dentro do pedido (<c>RemoverItem(idProduto)</c>).
        /// </remarks>
        [HttpDelete("{id:long}/itens/{idProduto:long}")]
        [ProducesResponseType(typeof(RemoverItemDoPedidoDeCompraSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverItem(
            long id,
            long idProduto,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new RemoverItemDoPedidoDeCompraEntrada(id, idProduto), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/aprovar")]
        [ProducesResponseType(typeof(AprovarPedidoDeCompraSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Aprovar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new AprovarPedidoDeCompraEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Efetivar é a transição com efeito fora do módulo: publica
        /// <c>PedidoDeCompraEfetivado</c>, que os handlers traduzem em entrada de
        /// estoque e título a pagar. Esses efeitos são assíncronos (outbox), então um
        /// 200 aqui confirma o pedido concluído, não os efeitos já aplicados.
        /// </remarks>
        [HttpPost("{id:long}/efetivar")]
        [ProducesResponseType(typeof(EfetivarPedidoDeCompraSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Efetivar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new EfetivarPedidoDeCompraEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/cancelar")]
        [ProducesResponseType(typeof(CancelarPedidoDeCompraSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new CancelarPedidoDeCompraEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
