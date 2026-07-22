using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Infraestrutura.Persistencia.Contexto;

namespace simple_erp.Infraestrutura.Persistencia.Outbox
{
    /// <summary>
    /// Lê a caixa de saída e entrega cada evento aos seus manipuladores.
    ///
    /// Três decisões merecem atenção, porque são o que separa um outbox de verdade de um
    /// laço que só chama handlers:
    ///
    /// 1) UM ESCOPO POR EVENTO. Cada evento roda com o seu próprio DbContext. Se um
    ///    manipulador falhar no meio do caminho, as alterações que ele deixou rastreadas
    ///    morrem junto com o escopo, em vez de vazarem para o próximo evento do lote.
    ///
    /// 2) UMA TRANSAÇÃO POR EVENTO. O efeito do manipulador e a marcação de "processado"
    ///    são confirmados juntos. Sem isso, uma queda entre os dois faria o evento ser
    ///    reentregue com o efeito já aplicado.
    ///
    /// 3) A FALHA É GRAVADA FORA DA TRANSAÇÃO. O rollback desfaria o próprio registro da
    ///    falha, então o contador de tentativas é incrementado em um escopo separado.
    ///
    /// Ainda assim a entrega é "pelo menos uma vez": se o processo morrer depois do
    /// commit e antes de o worker seguir adiante, nada se perde, mas um evento pode ser
    /// reentregue. Manipuladores precisam ser idempotentes — e é por isso que este é o
    /// contrato honesto de sistemas distribuídos, não uma limitação desta implementação.
    /// </summary>
    public sealed class ProcessadorDeEventosPendentes : IProcessadorDeEventosPendentes
    {
        /// <summary>
        /// Depois disto a linha para de ser tentada. Sem esse teto, um evento defeituoso
        /// (uma "poison message") seria retomado para sempre e travaria a fila atrás de
        /// si. A linha continua no banco, pendente e com o último erro registrado, à
        /// espera de análise.
        /// </summary>
        public const int MaximoDeTentativas = 5;

        /// <summary>Limite da coluna ultimo_erro.</summary>
        private const int LimiteDaMensagemDeErro = 2000;

        private readonly IServiceScopeFactory _fabricaDeEscopos;

        public ProcessadorDeEventosPendentes(IServiceScopeFactory fabricaDeEscopos)
        {
            _fabricaDeEscopos = fabricaDeEscopos;
        }

        public async Task<Resultado<int>> ProcessarLoteAsync(
            int tamanhoDoLote,
            CancellationToken cancellationToken = default)
        {
            var lote = tamanhoDoLote < 1 ? 1 : tamanhoDoLote;

            var pendentes = await ObterPendentesAsync(lote, cancellationToken);

            if (pendentes.EhFalha)
                return Resultado<int>.Falha(pendentes.Erros!);

            var despachados = 0;

            foreach (var idDaLinha in pendentes.Instancia)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (await ProcessarEventoAsync(idDaLinha, cancellationToken))
                    despachados++;
            }

            return Resultado<int>.Sucesso(despachados);
        }

        /// <summary>
        /// Devolve apenas os identificadores: cada evento será recarregado no seu próprio
        /// escopo, e trazer as entidades aqui só as deixaria presas a um contexto que
        /// será descartado.
        /// </summary>
        private async Task<Resultado<IReadOnlyList<long>>> ObterPendentesAsync(
            int tamanhoDoLote, CancellationToken cancellationToken)
        {
            try
            {
                await using var escopo = _fabricaDeEscopos.CreateAsyncScope();
                var contexto = escopo.ServiceProvider.GetRequiredService<SimpleErpDbContext>();

                var ids = await contexto.Set<EventoNoOutbox>()
                    .AsNoTracking()
                    .Where(linha =>
                        linha.ProcessadoEmUtc == null &&
                        linha.Tentativas < MaximoDeTentativas)
                    // Ordem de chegada: os eventos de um mesmo agregado são entregues na
                    // sequência em que o domínio os produziu.
                    .OrderBy(linha => linha.CriadoEmUtc)
                    .ThenBy(linha => linha.Id)
                    .Take(tamanhoDoLote)
                    .Select(linha => linha.Id)
                    .ToListAsync(cancellationToken);

                return Resultado<IReadOnlyList<long>>.Sucesso(ids);
            }
            catch (Exception ex)
            {
                return Resultado<IReadOnlyList<long>>.Falha(ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>Processa um evento. Devolve true apenas quando o despacho foi confirmado.</summary>
        private async Task<bool> ProcessarEventoAsync(long idDaLinha, CancellationToken cancellationToken)
        {
            await using var escopo = _fabricaDeEscopos.CreateAsyncScope();
            var provedor = escopo.ServiceProvider;

            var contexto = provedor.GetRequiredService<SimpleErpDbContext>();
            var unitOfWork = provedor.GetRequiredService<IUnitOfWork>();
            var dispatcher = provedor.GetRequiredService<IDispatcherDeEventos>();
            var logService = provedor.GetRequiredService<ILogService>();

            var linha = await contexto.Set<EventoNoOutbox>()
                .FirstOrDefaultAsync(evento => evento.Id == idDaLinha, cancellationToken);

            // Outra execução pode ter processado a linha entre a leitura do lote e agora.
            if (linha is null || !linha.EstaPendente)
                return false;

            var evento = SerializadorDeEventos.Desserializar(linha.TipoDoEvento, linha.Conteudo);

            if (evento is null)
            {
                await RegistrarFalhaAsync(
                    idDaLinha, $"EVENTO_NAO_REIDRATADO: {linha.TipoDoEvento}", cancellationToken);

                logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Não foi possível reidratar um evento da caixa de saída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["LinhaDoOutbox"] = idDaLinha,
                        ["TipoDoEvento"] = linha.TipoDoEvento
                    }));

                return false;
            }

            var transacao = await unitOfWork.BeginTransactionAsync(cancellationToken);

            if (transacao.EhFalha)
            {
                await RegistrarFalhaAsync(
                    idDaLinha, Juntar(transacao.Erros), cancellationToken);
                return false;
            }

            try
            {
                var despacho = await dispatcher.DespacharAsync(new[] { evento }, cancellationToken);

                if (despacho.EhFalha)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    await RegistrarFalhaAsync(idDaLinha, Juntar(despacho.Erros), cancellationToken);

                    logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Manipulador de evento falhou; a linha do outbox segue pendente.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["LinhaDoOutbox"] = idDaLinha,
                            ["Evento"] = linha.NomeDoEvento,
                            ["Erros"] = despacho.Erros?.ToArray()
                        }));

                    return false;
                }

                // A marcação entra na MESMA transação em que os manipuladores agiram.
                linha.MarcarComoProcessado();

                var persistencia = await unitOfWork.SaveChangesAsync(cancellationToken);

                if (persistencia.EhFalha)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    await RegistrarFalhaAsync(idDaLinha, Juntar(persistencia.Erros), cancellationToken);
                    return false;
                }

                var confirmacao = await unitOfWork.CommitTransactionAsync(cancellationToken);

                if (confirmacao.EhFalha)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    await RegistrarFalhaAsync(idDaLinha, Juntar(confirmacao.Erros), cancellationToken);
                    return false;
                }

                logService.RegistrarLogInformation(new RegistroDeLog(
                    Mensagem: "Evento de domínio despachado a partir da caixa de saída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["LinhaDoOutbox"] = idDaLinha,
                        ["Evento"] = linha.NomeDoEvento,
                        ["IdAgregadoOrigem"] = linha.IdAgregadoOrigem,
                        ["Tentativas"] = linha.Tentativas
                    }));

                return true;
            }
            catch (Exception ex)
            {
                // Exceção fora do contrato de Resultado (falha de rede, timeout do banco).
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                await RegistrarFalhaAsync(
                    idDaLinha, ex.InnerException?.Message ?? ex.Message, CancellationToken.None);

                return false;
            }
        }

        /// <summary>
        /// Incrementa o contador de tentativas em um escopo próprio.
        ///
        /// O escopo separado não é detalhe: o registro da falha acontece depois de um
        /// rollback, e gravá-lo no mesmo contexto significaria desfazê-lo junto. Sem essa
        /// separação, um evento defeituoso nunca acumularia tentativas e escaparia para
        /// sempre do teto de <see cref="MaximoDeTentativas"/>.
        /// </summary>
        private async Task RegistrarFalhaAsync(
            long idDaLinha, string erro, CancellationToken cancellationToken)
        {
            try
            {
                await using var escopo = _fabricaDeEscopos.CreateAsyncScope();
                var contexto = escopo.ServiceProvider.GetRequiredService<SimpleErpDbContext>();

                var linha = await contexto.Set<EventoNoOutbox>()
                    .FirstOrDefaultAsync(evento => evento.Id == idDaLinha, cancellationToken);

                if (linha is null)
                    return;

                linha.RegistrarFalha(Truncar(erro));

                await contexto.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Não há a quem escalar: falhar ao registrar a falha não pode derrubar o
                // worker. A linha continua pendente e será retomada.
            }
        }

        private static string Juntar(IEnumerable<string>? erros)
        {
            var texto = erros is null ? string.Empty : string.Join(" | ", erros);

            return string.IsNullOrWhiteSpace(texto) ? "ERRO_NAO_INFORMADO" : texto;
        }

        private static string Truncar(string erro) =>
            string.IsNullOrWhiteSpace(erro) ? "ERRO_NAO_INFORMADO"
            : erro.Length <= LimiteDaMensagemDeErro ? erro
            : erro[..LimiteDaMensagemDeErro];
    }
}
