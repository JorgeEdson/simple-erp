using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Core.Compartilhado.Eventos
{  
    public sealed class DispatcherDeEventos : IDispatcherDeEventos
    {
        private readonly IResolvedorDeManipuladores _resolvedor;
        private readonly ILogService _logService;

        public DispatcherDeEventos(
            IResolvedorDeManipuladores resolvedor,
            ILogService logService)
        {
            _resolvedor = resolvedor;
            _logService = logService;
        }

        public async Task<Resultado<bool>> DespacharAsync(
            IEnumerable<EventoDeDominio> eventos,
            CancellationToken cancellationToken = default)
        {
            if (eventos is null)
                return Resultado<bool>.Sucesso(true);

            var erros = new List<string>();

            foreach (var evento in eventos)
            {
                if (evento is null)
                    continue;

                var tipoEvento = evento.GetType();
                var manipuladores = _resolvedor.ResolverPara(tipoEvento);

                if (manipuladores.Count == 0)
                {
                    _logService.RegistrarLogInformation(new RegistroDeLog(
                        Mensagem: "Evento de domínio sem handlers registrados.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Evento"] = tipoEvento.Name,
                            ["IdEvento"] = evento.IdEvento.Valor
                        }));
                    continue;
                }

                foreach (var manipulador in manipuladores)
                {
                    var resultado = await InvocarAsync(manipulador, evento, cancellationToken);

                    if (resultado.EhFalha)
                    {
                        erros.AddRange(resultado.Erros!);

                        _logService.RegistrarLogWarning(new RegistroDeLog(
                            Mensagem: "Handler de evento de domínio retornou falha.",
                            Propriedades: new Dictionary<string, object?>
                            {
                                ["Evento"] = tipoEvento.Name,
                                ["Handler"] = manipulador.GetType().Name,
                                ["Erros"] = resultado.Erros?.ToArray()
                            }));
                    }
                    else
                    {
                        _logService.RegistrarLogDebug(new RegistroDeLog(
                            Mensagem: "Handler de evento de domínio executado com sucesso.",
                            Propriedades: new Dictionary<string, object?>
                            {
                                ["Evento"] = tipoEvento.Name,
                                ["Handler"] = manipulador.GetType().Name
                            }));
                    }
                }
            }

            return erros.Count > 0
                ? Resultado<bool>.Falha(erros)
                : Resultado<bool>.Sucesso(true);
        }

        
        /// <summary>
        /// Invoca o handler pelo tipo fechado da interface
        /// (IManipuladorDeEventoDeDominio&lt;TipoConcretoDoEvento&gt;) via reflection.
        /// Não usa `dynamic`: o binder de runtime só enxerga tipos públicos e
        /// falharia com handlers internal/privados ou proxies de teste — a
        /// invocação pela interface funciona para qualquer visibilidade.
        /// </summary>
        private static async Task<Resultado<bool>> InvocarAsync(
            object manipulador,
            EventoDeDominio evento,
            CancellationToken cancellationToken)
        {
            try
            {
                var tipoDaInterface = typeof(IManipuladorDeEventoDeDominio<>)
                    .MakeGenericType(evento.GetType());

                if (!tipoDaInterface.IsInstanceOfType(manipulador))
                    return Resultado<bool>.Falha("HANDLER_INCOMPATIVEL_COM_O_EVENTO");

                var metodo = tipoDaInterface.GetMethod(
                    nameof(IManipuladorDeEventoDeDominio<EventoDeDominio>.ManipularAsync))!;

                var tarefa = (Task<Resultado<bool>>)metodo.Invoke(
                    manipulador, new object?[] { evento, cancellationToken })!;

                return await tarefa;
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                // Exceção lançada DENTRO do handler — a causa real está na inner.
                return Resultado<bool>.Falha(ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                return Resultado<bool>.Falha(ex.Message);
            }
        }
    }
}
