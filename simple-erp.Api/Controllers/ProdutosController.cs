using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.CatalogoDeProdutos;
using simple_erp.Core.Compartilhado.Contratos.EventosDeDominio;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;

namespace simple_erp.Api.Controllers
{   
    [Route("api/produtos")]
    public sealed class ProdutosController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public ProdutosController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CadastrarProdutoSaida), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cadastrar(
            [FromBody] CadastrarProdutoRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new CadastrarProdutoEntrada(
                requisicao.Codigo,
                requisicao.Descricao,
                requisicao.UnidadeDeMedida,
                requisicao.Classificacao);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);

            return Criado(resultado, RotaObterPorId, saida => new { id = saida.Id });
        }

        // Nome de rota único na aplicação (usado pelo CreatedAtRoute do POST).
        private const string RotaObterPorId = "ObterProdutoPorId";

        [HttpGet("{id:guid}", Name = RotaObterPorId)]
        [ProducesResponseType(typeof(ObterProdutoPorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(Guid id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterProdutoPorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarProdutosPaginadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] string? codigo = null,
            [FromQuery] string? descricao = null,
            [FromQuery] string? unidadeDeMedida = null,
            [FromQuery] string? classificacao = null,
            [FromQuery] bool? ativo = null)
        {
            var entrada = new ListarProdutosPaginadoEntrada(
                numeroPagina, tamanhoPagina, codigo, descricao, unidadeDeMedida, classificacao, ativo);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(EditarProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Editar(
            Guid id,
            [FromBody] EditarProdutoRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new EditarProdutoEntrada(
                id,
                requisicao.Codigo,
                requisicao.Descricao,
                requisicao.UnidadeDeMedida);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:guid}/inativar")]
        [ProducesResponseType(typeof(InativarProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Inativar(Guid id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new InativarProdutoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:guid}/reativar")]
        [ProducesResponseType(typeof(ReativarProdutoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reativar(Guid id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ReativarProdutoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:guid}/classificar/fabricado")]
        [ProducesResponseType(typeof(ClassificarProdutoComoFabricadoSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClassificarComoFabricado(
            Guid id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ClassificarProdutoComoFabricadoEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:guid}/classificar/materia-prima")]
        [ProducesResponseType(typeof(ClassificarProdutoComoMateriaPrimaSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClassificarComoMateriaPrima(
            Guid id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ClassificarProdutoComoMateriaPrimaEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
