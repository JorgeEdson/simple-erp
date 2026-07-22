using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;

namespace simple_erp.Api.Controllers
{
    /// <summary>
    /// Endpoints do módulo Estoque. Só de leitura, de propósito: saldo e extrato são
    /// projeções do que os handlers de evento já registraram (compra efetivada, venda
    /// concluída, ordem de produção). Não há POST de movimentação — expor um furaria a
    /// consistência entre módulos, permitindo por HTTP o que só o domínio deve causar.
    /// </summary>
    [Route("api/estoque")]
    public sealed class EstoqueController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public EstoqueController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <remarks>
        /// Produto sem movimentação nenhuma não é 404: o use case devolve 200 com
        /// <c>PossuiRegistroDeSaldo = false</c> e quantidade zero. "Nunca movimentou"
        /// é uma resposta válida sobre o estoque, não ausência do recurso.
        /// </remarks>
        [HttpGet("produtos/{idProduto:long}/saldo")]
        [ProducesResponseType(typeof(ConsultarSaldoDeEstoqueSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConsultarSaldo(
            long idProduto,
            CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ConsultarSaldoDeEstoqueEntrada(idProduto), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet("movimentacoes")]
        [ProducesResponseType(typeof(ConsultarExtratoDeMovimentacoesPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConsultarExtrato(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] long? idProduto = null,
            [FromQuery] string? tipo = null,
            [FromQuery] string? origemTipo = null,
            [FromQuery] DateTime? dataInicio = null,
            [FromQuery] DateTime? dataFim = null)
        {
            // Os enums entram como texto para o contrato ficar legível e estável.
            // A conversão falha antes do use case, com o mesmo formato de erro do domínio.
            if (!TentarConverterEnum<TipoDeMovimentacao>(tipo, "TIPO_MOVIMENTACAO_INVALIDO", out var tipoConvertido, out var erroTipo))
                return BadRequest(erroTipo);

            if (!TentarConverterEnum<TipoOrigemMovimentacao>(origemTipo, "ORIGEM_TIPO_INVALIDO", out var origemConvertida, out var erroOrigem))
                return BadRequest(erroOrigem);

            var entrada = new ConsultarExtratoDeMovimentacoesPaginadoEntrada(
                numeroPagina,
                tamanhoPagina,
                idProduto,
                tipoConvertido,
                origemConvertida,
                dataInicio,
                dataFim);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }
    }
}
