using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.Vendas;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do agregado Pedido de Venda. Ciclo EmEdicao → Aprovado → Concluido,
    /// com Cancelado como saída.
    ///
    /// Atenção à assimetria com o pedido de compra: lá o efeito fora do módulo está em
    /// <c>efetivar</c> (a última transição); aqui está em <c>aprovar</c> (a primeira).
    /// Aprovar já baixa o estoque e emite o título a receber — concluir apenas fecha o
    /// pedido.
    /// </summary>
    [Route("api/pedidos-de-venda")]
    public sealed class PedidosDeVendaController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public PedidosDeVendaController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CriarPedidoDeVendaSaida), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar(
            [FromBody] CriarPedidoDeVendaRequest requisicao,
            CancellationToken cancellationToken)
        {
            // Lista ausente vira lista vazia: "pedido sem item" é regra do domínio, e é
            // ele quem deve recusá-la com o código dele.
            var itens = (requisicao.Itens ?? Array.Empty<ItemDePedidoDeVendaRequest>())
                .Select(item => new ItemPedidoDeVendaEntrada(
                    item.IdProduto,
                    item.Quantidade,
                    item.PrecoUnitario,
                    item.Desconto))
                .ToArray();

            var entrada = new CriarPedidoDeVendaEntrada(
                requisicao.IdCliente,
                itens,
                requisicao.DescontoDoPedido);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);

            return Criado(resultado, RotaObterPorId, saida => new { id = saida.Id });
        }

        // Nome de rota único na aplicação (usado pelo CreatedAtRoute do POST).
        private const string RotaObterPorId = "ObterPedidoDeVendaPorId";

        [HttpGet("{id:long}", Name = RotaObterPorId)]
        [ProducesResponseType(typeof(ObterPedidoDeVendaPorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterPedidoDeVendaPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarPedidosDeVendaPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] long? idCliente = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null)
        {
            if (!TentarConverterEnum<StatusPedidoDeVenda>(
                    status, "STATUS_PEDIDO_DE_VENDA_INVALIDO", out var statusConvertido, out var erroStatus))
                return BadRequest(erroStatus);

            var entrada = new ListarPedidosDeVendaPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                idCliente,
                statusConvertido,
                dataInicio,
                dataFim);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/itens")]
        [ProducesResponseType(typeof(AdicionarItemAoPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AdicionarItem(
            long id,
            [FromBody] AdicionarItemAoPedidoDeVendaRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new AdicionarItemAoPedidoDeVendaEntrada(
                id,
                requisicao.IdProduto,
                requisicao.Quantidade,
                requisicao.PrecoUnitario,
                requisicao.Desconto);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// O item é endereçado pelo produto, não por um id próprio: o domínio trata o
        /// produto como chave do item dentro do pedido.
        /// </remarks>
        [HttpDelete("{id:long}/itens/{idProduto:long}")]
        [ProducesResponseType(typeof(RemoverItemDoPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoverItem(
            long id,
            long idProduto,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new RemoverItemDoPedidoDeVendaEntrada(id, idProduto), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// PUT, não POST: o domínio faz <c>DescontoDoPedido = desconto</c>, substituindo
        /// o valor anterior. Repetir a chamada com o mesmo corpo dá o mesmo resultado.
        /// </remarks>
        [HttpPut("{id:long}/desconto")]
        [ProducesResponseType(typeof(AplicarDescontoNoPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AplicarDesconto(
            long id,
            [FromBody] AplicarDescontoNoPedidoDeVendaRequest requisicao,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new AplicarDescontoNoPedidoDeVendaEntrada(id, requisicao.Desconto), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Transição com efeito fora do módulo: publica <c>PedidoDeVendaAprovado</c>,
        /// que o <c>SaidaPorVendaHandler</c> traduz em baixa de estoque e o
        /// <c>GeracaoDeTituloAReceberHandler</c> em título a receber. Aprovar também
        /// congela valores — o pedido deixa de ser editável. Como o despacho é pelo
        /// outbox, o 200 confirma o pedido aprovado, não os efeitos já aplicados.
        /// </remarks>
        [HttpPost("{id:long}/aprovar")]
        [ProducesResponseType(typeof(AprovarPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Aprovar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new AprovarPedidoDeVendaEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/concluir")]
        [ProducesResponseType(typeof(ConcluirPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Concluir(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ConcluirPedidoDeVendaEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Único cancelamento do sistema que exige corpo: o motivo é obrigatório no
        /// domínio e viaja no evento <c>PedidoDeVendaCancelado</c>.
        /// </remarks>
        [HttpPost("{id:long}/cancelar")]
        [ProducesResponseType(typeof(CancelarPedidoDeVendaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancelar(
            long id,
            [FromBody] CancelarPedidoDeVendaRequest requisicao,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new CancelarPedidoDeVendaEntrada(id, requisicao.Motivo), cancellationToken);
            return Responder(resultado);
        }
    }
}
