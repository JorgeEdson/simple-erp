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

        /// <summary>
        /// Converte um filtro de query string no enum correspondente. Ausente
        /// (null/vazio) é filtro não informado, não erro. Valor numérico é recusado de
        /// propósito: aceitar "5" amarraria o contrato à ordem de declaração do enum.
        /// Em caso de falha, devolve <paramref name="codigoDeErro"/> no mesmo formato
        /// dos erros de domínio, para o cliente tratar por código e não por mensagem.
        /// </summary>
        protected static bool TentarConverterEnum<TEnum>(
            string? valor,
            string codigoDeErro,
            out TEnum? convertido,
            out RespostaDeErro? erro)
            where TEnum : struct, Enum
        {
            convertido = null;
            erro = null;

            if (string.IsNullOrWhiteSpace(valor))
                return true;

            var ehNumero = long.TryParse(valor, out _);

            if (!ehNumero && Enum.TryParse<TEnum>(valor, ignoreCase: true, out var resultado)
                && Enum.IsDefined(resultado))
            {
                convertido = resultado;
                return true;
            }

            erro = new RespostaDeErro(new[] { codigoDeErro });
            return false;
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
