using Microsoft.AspNetCore.Mvc;
using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Api.Comum
{
    
    [ApiController]
    public abstract class ControllerBaseDaApi : ControllerBase
    {   
        protected IActionResult Responder<T>(Resultado<T> resultado)
        {
            if (resultado.EhSucesso)
                return Ok(resultado.Instancia);

            return ParaErro(resultado);
        }
        
        protected IActionResult Criado<T>(
            Resultado<T> resultado, string nomeDaRota, Func<T, object> valoresDaRota)
        {
            if (resultado.EhSucesso)
                return CreatedAtRoute(nomeDaRota, valoresDaRota(resultado.Instancia), resultado.Instancia);

            return ParaErro(resultado);
        }

        private IActionResult ParaErro<T>(Resultado<T> resultado)
        {
            var erros = resultado.Erros?.ToArray() ?? Array.Empty<string>();

            var ehNaoEncontrado = erros.Any(erro =>
                erro.Contains("NAO_ENCONTRADO", StringComparison.OrdinalIgnoreCase)
                || erro.Contains("NAO_ENCONTRADA", StringComparison.OrdinalIgnoreCase));

            var corpo = new RespostaDeErro(erros);

            return ehNaoEncontrado
                ? NotFound(corpo)
                : BadRequest(corpo);
        }
    }
}
