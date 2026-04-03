namespace simple_erp.Core.Compartilhado.Interfaces
{
    public interface ILogService
    {
        void RegistrarLogDebug(RegistroDeLog registroDeLog);
        void RegistrarLogInformation(RegistroDeLog registroDeLog);
        void RegistrarLogWarning(RegistroDeLog registroDeLog);
        void RegistrarLogError(RegistroDeLog registroDeLog);
        void RegistrarLogCritical(RegistroDeLog registroDeLog);
        IDisposable IniciarEscopo(IReadOnlyDictionary<string, object?> propriedades);
    }

    public sealed record RegistroDeLog(
        string Mensagem,
        IReadOnlyDictionary<string, object?>? Propriedades = null,
        Exception? Exception = null);
}
