using simple_erp.Core.Compartilhado.Base;
using System;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Estoque.ObjetosDeValor
{
    /// <summary>
    /// Origem (documento de referência) que deu causa à movimentação, usada para
    /// consultar o extrato "por origem": um pedido de compra, um pedido de venda,
    /// uma ordem de produção ou um ajuste manual.
    /// </summary>
    public enum TipoOrigemMovimentacao
    {
        Compra = 1,
        Venda = 2,
        Producao = 3,
        AjusteManual = 4,
        Outro = 0
    }

    public sealed class OrigemDaMovimentacao
        : ObjetoDeValor<PropriedadesOrigemDaMovimentacao, IConfiguracaoObjetoDeValor>
    {
        private OrigemDaMovimentacao(
            PropriedadesOrigemDaMovimentacao valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<OrigemDaMovimentacao> TentarCriar(
            TipoOrigemMovimentacao tipo,
            long? idReferencia = null,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            if (idReferencia.HasValue && idReferencia.Value <= 0)
                return Resultado<OrigemDaMovimentacao>.Falha("ORIGEM_REFERENCIA_INVALIDA");

            var propriedades = new PropriedadesOrigemDaMovimentacao(tipo, idReferencia);

            return Resultado<OrigemDaMovimentacao>.Sucesso(
                new OrigemDaMovimentacao(propriedades, configuracao));
        }

        public TipoOrigemMovimentacao Tipo => Valor.Tipo;
        public long? IdReferencia => Valor.IdReferencia;

        public override string ToString()
        {
            return IdReferencia.HasValue ? $"{Tipo}[{IdReferencia}]" : Tipo.ToString();
        }
    }

    public record PropriedadesOrigemDaMovimentacao(
        TipoOrigemMovimentacao Tipo,
        long? IdReferencia);
}
