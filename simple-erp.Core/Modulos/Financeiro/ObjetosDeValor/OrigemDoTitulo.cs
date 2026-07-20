using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Core.Modulos.Financeiro.ObjetosDeValor
{
    /// <summary>
    /// Documento que deu origem ao título (para rastreabilidade): um pedido de compra,
    /// um pedido de venda, ou uma origem avulsa.
    /// </summary>
    public enum TipoOrigemTitulo
    {
        Compra = 1,
        Venda = 2,
        Avulso = 0
    }

    public sealed class OrigemDoTitulo
        : ObjetoDeValor<PropriedadesOrigemDoTitulo, IConfiguracaoObjetoDeValor>
    {
        private OrigemDoTitulo(
            PropriedadesOrigemDoTitulo valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<OrigemDoTitulo> TentarCriar(
            TipoOrigemTitulo tipo,
            long? idReferencia = null,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            if (idReferencia.HasValue && idReferencia.Value <= 0)
                return Resultado<OrigemDoTitulo>.Falha("ORIGEM_REFERENCIA_INVALIDA");

            var propriedades = new PropriedadesOrigemDoTitulo(tipo, idReferencia);

            return Resultado<OrigemDoTitulo>.Sucesso(new OrigemDoTitulo(propriedades, configuracao));
        }

        public TipoOrigemTitulo Tipo => Valor.Tipo;
        public long? IdReferencia => Valor.IdReferencia;

        public override string ToString()
        {
            return IdReferencia.HasValue ? $"{Tipo}[{IdReferencia}]" : Tipo.ToString();
        }
    }

    public record PropriedadesOrigemDoTitulo(
        TipoOrigemTitulo Tipo,
        long? IdReferencia);
}
