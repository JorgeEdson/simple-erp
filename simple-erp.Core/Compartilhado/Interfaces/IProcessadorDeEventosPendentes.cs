using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Compartilhado.Interfaces
{
    /// <summary>
    /// Consome os eventos que ficaram pendentes de despacho e os entrega aos seus
    /// manipuladores.
    ///
    /// A abstração existe para separar duas responsabilidades que costumam ser
    /// confundidas: QUANDO processar (uma decisão de hospedagem — um worker em segundo
    /// plano, um agendador, um endpoint administrativo) e COMO processar (uma decisão de
    /// persistência — ler a caixa de saída, reidratar o evento, marcar o resultado).
    ///
    /// Quem hospeda depende apenas deste contrato e não conhece a tabela de outbox.
    /// </summary>
    public interface IProcessadorDeEventosPendentes
    {
        /// <summary>
        /// Processa até <paramref name="tamanhoDoLote"/> eventos pendentes e devolve
        /// quantos foram despachados com sucesso.
        /// </summary>
        Task<Resultado<int>> ProcessarLoteAsync(
            int tamanhoDoLote,
            CancellationToken cancellationToken = default);
    }
}
