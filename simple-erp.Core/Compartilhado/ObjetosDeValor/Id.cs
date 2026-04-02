using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.ObjetosDeValor
{
    public sealed class Id : ObjetoDeValor<long, IConfiguracaoObjetoDeValor>
    {
        private const string IdInvalido = "ID_INVALIDO";

        private static long _ultimoTimestamp = -1L;
        private static int _contador = 0;
        private static readonly object _bloqueio = new();

        private Id(long valor, IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        

        public static Resultado<Id> TentarCriar(
            long? valor = null,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            try
            {
                var valorId = valor ?? GerarProximoValor();

                if (valorId <= 0)
                    return Resultado<Id>.Falha(IdInvalido);

                return Resultado<Id>.Sucesso(new Id(valorId, configuracao));
            }
            catch (Exception ex)
            {
                return Resultado<Id>.Falha(ex.Message);
            }
        }        

        private static long GerarProximoValor()
        {
            try
            {
                lock (_bloqueio)
                {
                    var timestamp = ObterTimestampAtual();

                    if (timestamp == _ultimoTimestamp)
                    {
                        _contador++;
                    }
                    else
                    {
                        _contador = 0;
                        _ultimoTimestamp = timestamp;
                    }

                    return long.Parse($"{timestamp}{_contador:D4}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar o próximo ID.", ex);
            }
        }
        private static long ObterTimestampAtual()
        {
            return long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        }
    }
}
