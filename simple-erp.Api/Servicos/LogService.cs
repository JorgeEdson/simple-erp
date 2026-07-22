using simple_erp.Core.Compartilhado.Interfaces;

namespace simple_erp.Api.Servicos
{   
    public sealed class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
        }

        public void RegistrarLogDebug(RegistroDeLog registro) =>
            Registrar(LogLevel.Debug, registro);

        public void RegistrarLogInformation(RegistroDeLog registro) =>
            Registrar(LogLevel.Information, registro);

        public void RegistrarLogWarning(RegistroDeLog registro) =>
            Registrar(LogLevel.Warning, registro);

        public void RegistrarLogError(RegistroDeLog registro) =>
            Registrar(LogLevel.Error, registro);

        public void RegistrarLogCritical(RegistroDeLog registro) =>
            Registrar(LogLevel.Critical, registro);

        public IDisposable IniciarEscopo(IReadOnlyDictionary<string, object?> propriedades) =>
            _logger.BeginScope(propriedades) ?? NullDisposable.Instancia;

        private void Registrar(LogLevel nivel, RegistroDeLog registro)
        {
            if (!_logger.IsEnabled(nivel))
                return;

            using var escopo = registro.Propriedades is { Count: > 0 }
                ? _logger.BeginScope(registro.Propriedades)
                : null;

            _logger.Log(nivel, registro.Exception, "{Mensagem}", registro.Mensagem);
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instancia = new();
            public void Dispose() { }
        }
    }
}
