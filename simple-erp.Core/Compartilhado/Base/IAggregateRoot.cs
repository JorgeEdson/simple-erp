namespace simple_erp.Core.Compartilhado.Base
{
    /// <summary>
    /// Interface marcadora (marker interface) que identifica a <b>Raiz de um Agregado</b>
    /// (Aggregate Root) no modelo de domínio.
    ///
    /// <para>
    /// <b>Entidade x Agregado — a distinção que esta interface torna explícita:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Toda classe que herda de <see cref="Entidade{TEntidade}"/> é uma <b>Entity</b>:
    ///     tem identidade própria (<c>Id</c>) e ciclo de vida. Porém, ser Entity não
    ///     significa poder ser acessada ou persistida isoladamente.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Um <b>Agregado</b> é um conjunto de entidades e objetos de valor tratado como
    ///     uma única unidade de consistência. A <b>Raiz do Agregado</b> é a única Entity
    ///     do conjunto que pode ser referenciada de fora: ela é o ponto de entrada que
    ///     protege as invariantes e é carregada/salva como um todo por um repositório próprio.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Regra prática do projeto: <b>somente</b> tipos marcados com <see cref="IAggregateRoot"/>
    /// possuem repositório e são a fronteira de uma transação. Uma Entity sem esta interface
    /// (por exemplo, <c>ParceiroComercial</c>, que é abstrata e não tem repositório) é uma
    /// Entity que <b>não</b> é raiz de agregado.
    /// </para>
    ///
    /// <para>
    /// É proposital que esta interface não declare membros: seu papel é apenas expressar a
    /// intenção do modelo no próprio código, permitindo distinguir raízes de agregado das
    /// demais entidades sem depender apenas de convenção ou documentação.
    /// </para>
    /// </summary>
    public interface IAggregateRoot
    {
    }
}
