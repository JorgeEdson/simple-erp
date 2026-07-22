using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.Producao;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do agregado Ordem de Produção. O ciclo é Criada → Confirmada →
    /// Concluida, com Cancelada como saída, e cada transição é um POST próprio pelo
    /// mesmo motivo do pedido de compra: quem chama pede a ação, o agregado decide se
    /// ela vale no estado atual.
    /// </summary>
    [Route("api/ordens-de-producao")]
    public sealed class OrdensDeProducaoController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public OrdensDeProducaoController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <remarks>
        /// As necessidades de matéria-prima não vêm no corpo: são calculadas pelo
        /// domínio a partir da composição ativa do produto. Criar uma ordem para um
        /// produto sem receita ativa é recusado pelo use case, não pelo controller.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(CriarOrdemDeProducaoSaida), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar(
            [FromBody] CriarOrdemDeProducaoRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new CriarOrdemDeProducaoEntrada(
                requisicao.IdProdutoFabricado,
                requisicao.QuantidadeAProduzir);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);

            return Criado(resultado, RotaObterPorId, saida => new { id = saida.Id });
        }

        // Nome de rota único na aplicação (usado pelo CreatedAtRoute do POST).
        private const string RotaObterPorId = "ObterOrdemDeProducaoPorId";

        [HttpGet("{id:long}", Name = RotaObterPorId)]
        [ProducesResponseType(typeof(ObterOrdemDeProducaoPorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterOrdemDeProducaoPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarOrdensDeProducaoPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] long? idProdutoFabricado = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null)
        {
            if (!TentarConverterEnum<StatusOrdemDeProducao>(
                    status, "STATUS_ORDEM_DE_PRODUCAO_INVALIDO", out var statusConvertido, out var erroStatus))
                return BadRequest(erroStatus);

            var entrada = new ListarOrdensDeProducaoPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                idProdutoFabricado,
                statusConvertido,
                dataInicio,
                dataFim);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/confirmar")]
        [ProducesResponseType(typeof(ConfirmarOrdemDeProducaoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirmar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ConfirmarOrdemDeProducaoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Concluir é a transição com efeito fora do módulo: publica
        /// <c>OrdemDeProducaoConcluida</c>, que o <c>MovimentacoesPorProducaoHandler</c>
        /// traduz em duas movimentações de estoque — saída dos insumos e entrada do
        /// produto acabado. Como o despacho é pelo outbox, o 200 confirma a ordem
        /// concluída, não o estoque já movimentado.
        /// </remarks>
        [HttpPost("{id:long}/concluir")]
        [ProducesResponseType(typeof(ConcluirOrdemDeProducaoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Concluir(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ConcluirOrdemDeProducaoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/cancelar")]
        [ProducesResponseType(typeof(CancelarOrdemDeProducaoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new CancelarOrdemDeProducaoEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
