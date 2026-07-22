using Microsoft.AspNetCore.Mvc;
using simple_erp.Api.Comum;
using simple_erp.Api.Modelos.ParceirosComerciais;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;

namespace simple_erp.Api.Controllers
{   
    [Route("api/clientes")]
    public sealed class ClientesController : ControllerBaseDaApi
    {
        private readonly IDispatcher _dispatcher;

        public ClientesController(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CadastrarClienteSaida), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cadastrar(
            [FromBody] ParceiroRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new CadastrarClienteEntrada(
                requisicao.Documento,
                requisicao.Nome,
                requisicao.Email,
                requisicao.Rua,
                requisicao.Numero,
                requisicao.Complemento,
                requisicao.Bairro,
                requisicao.Cidade,
                requisicao.Estado,
                requisicao.Cep,
                requisicao.Pais);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);

            return Criado(resultado, RotaObterPorId, saida => new { id = saida.Id });
        }

        // Nome de rota único na aplicação (usado pelo CreatedAtRoute do POST).
        private const string RotaObterPorId = "ObterClientePorId";

        [HttpGet("{id:long}", Name = RotaObterPorId)]
        [ProducesResponseType(typeof(ObterClientePorIdSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ObterClientePorIdEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpGet]
        [ProducesResponseType(typeof(ListarClientesPaginadoSaida), StatusCodes.Status200OK)]
        public async Task<IActionResult> Listar(
            CancellationToken cancellationToken,
            [FromQuery] int numeroPagina = 1,
            [FromQuery] int tamanhoPagina = 10,
            [FromQuery] string? nome = null,
            [FromQuery] string? documento = null,
            [FromQuery] string? email = null,
            [FromQuery] bool? ativo = null,
            [FromQuery] string? cidade = null,
            [FromQuery] string? estado = null)
        {
            var entrada = new ListarClientesPaginadoEntrada(
                numeroPagina, tamanhoPagina, nome, documento, email, ativo, cidade, estado);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(EditarClienteSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Editar(
            long id,
            [FromBody] ParceiroRequest requisicao,
            CancellationToken cancellationToken)
        {
            var entrada = new EditarClienteEntrada(
                id,
                requisicao.Documento,
                requisicao.Nome,
                requisicao.Email,
                requisicao.Rua,
                requisicao.Numero,
                requisicao.Complemento,
                requisicao.Bairro,
                requisicao.Cidade,
                requisicao.Estado,
                requisicao.Cep,
                requisicao.Pais);

            var resultado = await _dispatcher.EnviarAsync(entrada, cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/inativar")]
        [ProducesResponseType(typeof(InativarClienteSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Inativar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new InativarClienteEntrada(id), cancellationToken);
            return Responder(resultado);
        }

        [HttpPost("{id:long}/reativar")]
        [ProducesResponseType(typeof(ReativarClienteSaida), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RespostaDeErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reativar(long id, CancellationToken cancellationToken)
        {
            var resultado = await _dispatcher.EnviarAsync(
                new ReativarClienteEntrada(id), cancellationToken);
            return Responder(resultado);
        }
    }
}
