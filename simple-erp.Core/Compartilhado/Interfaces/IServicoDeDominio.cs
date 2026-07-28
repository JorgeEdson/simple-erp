namespace simple_erp.Core.Compartilhado.Interfaces
{
    /// <summary>
    /// Interface marcadora (marker interface) que identifica um <b>Serviço de Domínio</b>
    /// (Domain Service).
    ///
    /// <para>
    /// Um Domain Service abriga uma <b>regra de negócio que não pertence naturalmente a
    /// nenhuma entidade ou objeto de valor</b> — tipicamente porque a decisão envolve
    /// <b>mais de um agregado</b> ao mesmo tempo. Ele contém lógica de domínio (uma decisão
    /// do negócio), e não orquestração de aplicação (transação, log, mapeamento de DTO),
    /// que continua sendo responsabilidade do caso de uso.
    /// </para>
    ///
    /// <para>
    /// <b>Como distinguir de um caso de uso:</b> se a regra decide algo sobre o domínio
    /// combinando dados de agregados distintos (por exemplo, "há saldo suficiente de cada
    /// insumo para esta ordem de produção?", cruzando <c>OrdemDeProducao</c> e vários
    /// <c>SaldoDeEstoque</c>), ela é um Serviço de Domínio. Se apenas coordena repositórios,
    /// transações e efeitos colaterais, é um caso de uso.
    /// </para>
    ///
    /// <para>
    /// <b>Cuidado (evitar modelo anêmico):</b> só extraia para um Serviço de Domínio a
    /// lógica que realmente não cabe em um único agregado. Regras que operam sobre os dados
    /// de um agregado só devem permanecer nele (ex.: <c>ComposicaoDeProduto.CalcularNecessidades</c>).
    /// </para>
    ///
    /// <para>
    /// A interface não declara membros de propósito: serve para tornar explícita a intenção
    /// no código e permitir o registro por convenção no contêiner de injeção de dependência,
    /// da mesma forma que <c>IRepositorio</c> faz para os repositórios.
    /// </para>
    /// </summary>
    public interface IServicoDeDominio
    {
    }
}
