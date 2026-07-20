using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Estoque.Entidades
{ 
    public sealed class MovimentacaoDeEstoque : Entidade<MovimentacaoDeEstoque>
    {
        private MovimentacaoDeEstoque(
            Id idProduto,
            TipoDeMovimentacao tipo,
            SentidoDaMovimentacao sentido,
            decimal quantidade,
            decimal saldoResultante,
            OrigemDaMovimentacao origem,
            DateTime dataMovimentacaoUtc,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            IdProduto = idProduto;
            Tipo = tipo;
            Sentido = sentido;
            Quantidade = quantidade;
            SaldoResultante = saldoResultante;
            Origem = origem;
            DataMovimentacaoUtc = dataMovimentacaoUtc;
        }

        public Id IdProduto { get; private set; }
        public TipoDeMovimentacao Tipo { get; private set; }
        public SentidoDaMovimentacao Sentido { get; private set; }
        public decimal Quantidade { get; private set; }
        public decimal SaldoResultante { get; private set; }
        public OrigemDaMovimentacao Origem { get; private set; }
        public DateTime DataMovimentacaoUtc { get; private set; }

        public bool EhEntrada => Sentido == SentidoDaMovimentacao.Entrada;
        public bool EhSaida => Sentido == SentidoDaMovimentacao.Saida;

        public static Resultado<MovimentacaoDeEstoque> Criar(
            Id idProduto,
            TipoDeMovimentacao tipo,
            Quantidade quantidade,
            decimal saldoResultante,
            OrigemDaMovimentacao origem,
            long? id = null)
        {
            var erros = new List<string>();

            if (idProduto is null)
                erros.Add("PRODUTO_OBRIGATORIO");

            if (quantidade is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (origem is null)
                erros.Add("ORIGEM_OBRIGATORIA");

            if (erros.Count > 0)
                return Resultado<MovimentacaoDeEstoque>.Falha(erros);

            var movimentacao = new MovimentacaoDeEstoque(
                idProduto!,
                tipo,
                TiposDeMovimentacao.Sentido(tipo),
                quantidade!.Valor,
                saldoResultante,
                origem!,
                DateTime.UtcNow,
                id);

            return Resultado<MovimentacaoDeEstoque>.Sucesso(movimentacao);
        }

        public static MovimentacaoDeEstoque Reconstituir(
            Id idProduto,
            TipoDeMovimentacao tipo,
            SentidoDaMovimentacao sentido,
            decimal quantidade,
            decimal saldoResultante,
            OrigemDaMovimentacao origem,
            DateTime dataMovimentacaoUtc,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new MovimentacaoDeEstoque(
                idProduto,
                tipo,
                sentido,
                quantidade,
                saldoResultante,
                origem,
                dataMovimentacaoUtc,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
