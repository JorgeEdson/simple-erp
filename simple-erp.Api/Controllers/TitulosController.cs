using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.Financeiro;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do agregado Título (módulo Financeiro). Não há POST de emissão: os
    /// dois use cases de emissão são consumidos apenas pelos handlers de evento
    /// (pedido de compra efetivado / pedido de venda concluído) e ambos fixam a origem
    /// como Compra ou Venda. Emitir por HTTP produziria um título marcado como vindo de
    /// uma compra que não existe. Baixa e cancelamento, ao contrário, são ações de
    /// usuário — nenhum handler as dispara, então precisam de porta HTTP.
    /// </summary>
    [Route("api/financeiro/titulos")]
    public sealed class TitulosController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public TitulosController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ObterTituloPorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterTituloPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarTitulosPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] string? tipo = null,
            [FromQuery] string? status = null,
            [FromQuery] long? idParceiro = null,
            [FromQuery] DateTime? vencimentoInicio = null,
            [FromQuery] DateTime? vencimentoFim = null)
        {
            // Enums entram como texto ("APagar", "EmAberto") para o contrato ficar
            // legível e não depender da ordem de declaração.
            if (!TentarConverterEnum<TipoDeTitulo>(tipo, "TIPO_TITULO_INVALIDO", out var tipoConvertido, out var erroTipo))
                return BadRequest(erroTipo);

            if (!TentarConverterEnum<StatusTitulo>(status, "STATUS_TITULO_INVALIDO", out var statusConvertido, out var erroStatus))
                return BadRequest(erroStatus);

            var entrada = new ListarTitulosPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                tipoConvertido,
                statusConvertido,
                idParceiro,
                vencimentoInicio,
                vencimentoFim);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        /// <remarks>
        /// Baixa parcial é esperada: o título aceita várias, e a resposta traz o
        /// <c>SaldoDevedor</c> restante e o status resultante (ParcialmenteBaixado ou
        /// Liquidado). Por isso a rota é uma coleção — cada POST acrescenta uma baixa,
        /// não substitui a anterior.
        /// </remarks>
        [HttpPost("{id:long}/baixas")]
        [ProducesResponseType(typeof(BaixarTituloSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Baixar(
            long id,
            [FromBody] BaixarTituloRequest requisicao,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new BaixarTituloEntrada(id, requisicao.Valor), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/cancelar")]
        [ProducesResponseType(typeof(CancelarTituloSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new CancelarTituloEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
