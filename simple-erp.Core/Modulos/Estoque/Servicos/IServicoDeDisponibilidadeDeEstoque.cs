using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;

namespace simple_erp.Core.Modulos.Estoque.Servicos
{
    /// <summary>
    /// Uma quantidade requerida de um produto, usada como entrada neutra para a verificação
    /// de disponibilidade. É deliberadamente independente de qualquer módulo consumidor
    /// (Produção, Vendas, etc.) para que o serviço de estoque não dependa deles.
    /// </summary>
    public sealed record RequisicaoDeDisponibilidade(
        long IdProduto,
        decimal QuantidadeRequerida);

    /// <summary>
    /// Fato apurado para um produto cujo saldo não cobre a quantidade requerida.
    /// </summary>
    public sealed record InsuficienciaDeEstoque(
        long IdProduto,
        decimal QuantidadeRequerida,
        decimal QuantidadeDisponivel);

    /// <summary>
    /// Veredito neutro da verificação: a lista de insuficiências apuradas. Não carrega
    /// nenhum código de erro nem mensagem — a tradução do fato ("faltou saldo") para o
    /// vocabulário de cada contexto (INSUMO_INSUFICIENTE na Produção, PRODUTO_INSUFICIENTE
    /// em Vendas) é responsabilidade do caso de uso que consome o serviço.
    /// </summary>
    public sealed record VerificacaoDeDisponibilidade(
        IReadOnlyCollection<InsuficienciaDeEstoque> Insuficiencias)
    {
        public bool HaDisponibilidade => Insuficiencias.Count == 0;
    }

    /// <summary>
    /// <b>Serviço de Domínio</b> do módulo de Estoque. Responde a uma pergunta de negócio
    /// que <b>não pertence a nenhum agregado isolado</b>: dado um conjunto de necessidades,
    /// existe saldo suficiente de cada produto? A decisão cruza vários agregados
    /// <c>SaldoDeEstoque</c> (um por produto), então não cabe nem em <c>SaldoDeEstoque</c>
    /// (que só conhece a própria quantidade) nem no agregado que originou as necessidades.
    ///
    /// <para>
    /// Por ser reutilizado por mais de um contexto (Produção e Vendas), o serviço devolve
    /// um <see cref="VerificacaoDeDisponibilidade"/> neutro (dados), e não strings de erro:
    /// cada caso de uso mantém o seu próprio vocabulário na resposta.
    /// </para>
    /// </summary>
    public interface IServicoDeDisponibilidadeDeEstoque : IServicoDeDominio
    {
        /// <summary>
        /// Apura, para cada requisição, se o saldo cobre a quantidade requerida. Falhas de
        /// infraestrutura ou de validação (ex.: id inválido, erro no repositório) são
        /// propagadas como <see cref="Resultado{T}"/> de falha; a ausência de saldo suficiente
        /// não é falha, e sim parte do veredito de sucesso.
        /// </summary>
        Task<Resultado<VerificacaoDeDisponibilidade>> VerificarDisponibilidadeAsync(
            IEnumerable<RequisicaoDeDisponibilidade> requisicoes,
            CancellationToken cancellationToken = default);
    }
}
