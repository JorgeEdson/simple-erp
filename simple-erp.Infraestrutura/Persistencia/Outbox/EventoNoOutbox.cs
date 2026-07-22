using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Infraestrutura.Persistencia.Outbox
{
    /// <summary>
    /// Registro de um evento de domínio na caixa de saída (Outbox).
    ///
    /// A linha é gravada na MESMA transação que altera o agregado. Isso é o coração do
    /// padrão: ou o agregado e o evento são salvos juntos, ou nenhum dos dois é. Some
    /// assim a janela em que a operação é confirmada mas o efeito colateral se perde.
    ///
    /// Note que este NÃO é um agregado de domínio — é um artefato de persistência. Por
    /// isso não herda de Entidade&lt;T&gt;, não emite eventos, e sua chave é gerada pelo
    /// banco (identity), ao contrário dos agregados, cujo Id é gerado pela aplicação.
    ///
    /// Além de garantir a entrega, a tabela funciona como registro histórico do que
    /// aconteceu no domínio — o catálogo de eventos deixa de ser só documentação.
    /// </summary>
    public sealed class EventoNoOutbox
    {
        // Construtor de materialização do EF Core.
        private EventoNoOutbox()
        {
            NomeDoEvento = string.Empty;
            TipoDoEvento = string.Empty;
            Conteudo = string.Empty;
        }

        private EventoNoOutbox(
            long idEvento,
            string nomeDoEvento,
            string tipoDoEvento,
            long idAgregadoOrigem,
            string conteudo,
            DateTime ocorridoEmUtc)
        {
            IdEvento = idEvento;
            NomeDoEvento = nomeDoEvento;
            TipoDoEvento = tipoDoEvento;
            IdAgregadoOrigem = idAgregadoOrigem;
            Conteudo = conteudo;
            OcorridoEmUtc = ocorridoEmUtc;
            CriadoEmUtc = DateTime.UtcNow;
            Tentativas = 0;
        }

        /// <summary>Chave da linha, gerada pelo banco (sequência).</summary>
        public long Id { get; private set; }

        /// <summary>Identificador do evento no domínio, preservado do original.</summary>
        public long IdEvento { get; private set; }

        /// <summary>Nome curto do evento (ex.: "PedidoDeCompraEfetivado"), para leitura e filtro.</summary>
        public string NomeDoEvento { get; private set; }

        /// <summary>Nome completo do tipo, usado para reidratar o evento no despacho.</summary>
        public string TipoDoEvento { get; private set; }

        /// <summary>Agregado que originou o evento.</summary>
        public long IdAgregadoOrigem { get; private set; }

        /// <summary>Payload do evento em JSON (coluna jsonb).</summary>
        public string Conteudo { get; private set; }

        public DateTime OcorridoEmUtc { get; private set; }
        public DateTime CriadoEmUtc { get; private set; }

        /// <summary>Nulo enquanto pendente; preenchido quando o despacho conclui.</summary>
        public DateTime? ProcessadoEmUtc { get; private set; }

        public int Tentativas { get; private set; }

        public string? UltimoErro { get; private set; }

        public bool EstaPendente => ProcessadoEmUtc is null;

        public static EventoNoOutbox APartirDe(EventoDeDominio evento)
        {
            ArgumentNullException.ThrowIfNull(evento);

            return new EventoNoOutbox(
                idEvento: evento.IdEvento.Valor,
                nomeDoEvento: evento.GetType().Name,
                tipoDoEvento: evento.GetType().FullName!,
                idAgregadoOrigem: evento.IdAgregadoOrigem.Valor,
                conteudo: SerializadorDeEventos.Serializar(evento),
                ocorridoEmUtc: evento.DataOcorrenciaUtc);
        }

        /// <summary>Marca o evento como despachado com sucesso.</summary>
        public void MarcarComoProcessado()
        {
            ProcessadoEmUtc = DateTime.UtcNow;
            UltimoErro = null;
        }

        /// <summary>
        /// Registra uma tentativa malsucedida. A linha continua pendente, para ser
        /// retomada — é isso que dá ao Outbox a garantia de entrega ("pelo menos uma vez").
        /// </summary>
        public void RegistrarFalha(string erro)
        {
            Tentativas++;
            UltimoErro = erro;
        }
    }
}
