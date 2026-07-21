using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Eventos;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System;

namespace simple_erp.Core.Modulos.Estoque.Entidades
{
    /// <summary>
    /// Raiz de agregado que mantém o saldo de estoque de um produto. É o único ponto
    /// que altera a quantidade em estoque e onde vive a invariante operacional:
    /// impedir saída sem saldo, a menos que o saldo negativo seja permitido por
    /// configuração. Cada operação produz uma MovimentacaoDeEstoque consistente com
    /// o saldo resultante.
    /// </summary>
    public sealed class SaldoDeEstoque : Entidade<SaldoDeEstoque>
    {
#pragma warning disable CS8618 // Construtor de materialização do EF Core: as propriedades são preenchidas pelo provider.
        /// <summary>Construtor de materialização do EF Core.</summary>
        private SaldoDeEstoque()
        {
        }
#pragma warning restore CS8618

        private SaldoDeEstoque(
            Id idProduto,
            decimal quantidadeAtual,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            IdProduto = idProduto;
            QuantidadeAtual = quantidadeAtual;
        }

        public Id IdProduto { get; private set; }
        public decimal QuantidadeAtual { get; private set; }

        public bool PossuiSaldo => QuantidadeAtual > 0m;
        public bool EstaNegativo => QuantidadeAtual < 0m;

        public static Resultado<SaldoDeEstoque> Criar(Id idProduto, long? id = null)
        {
            if (idProduto is null)
                return Resultado<SaldoDeEstoque>.Falha("PRODUTO_OBRIGATORIO");

            var saldo = new SaldoDeEstoque(idProduto, 0m, id);

            saldo.AdicionarEventoDeDominio(new SaldoDeEstoqueCriado(saldo.Id, idProduto));

            return Resultado<SaldoDeEstoque>.Sucesso(saldo);
        }

        /// <summary>
        /// Aplica uma movimentação ao saldo. Para saídas, respeita a regra de saldo
        /// insuficiente conforme <paramref name="permitirSaldoNegativo"/>. Retorna o
        /// registro de extrato correspondente à operação.
        /// </summary>
        public Resultado<MovimentacaoDeEstoque> Movimentar(
            TipoDeMovimentacao tipo,
            Quantidade quantidade,
            OrigemDaMovimentacao origem,
            bool permitirSaldoNegativo = false)
        {
            if (quantidade is null)
                return Resultado<MovimentacaoDeEstoque>.Falha("QUANTIDADE_OBRIGATORIA");

            if (origem is null)
                return Resultado<MovimentacaoDeEstoque>.Falha("ORIGEM_OBRIGATORIA");

            var sentido = TiposDeMovimentacao.Sentido(tipo);

            var novoSaldo = sentido == SentidoDaMovimentacao.Entrada
                ? QuantidadeAtual + quantidade.Valor
                : QuantidadeAtual - quantidade.Valor;

            if (sentido == SentidoDaMovimentacao.Saida
                && novoSaldo < 0m
                && !permitirSaldoNegativo)
            {
                return Resultado<MovimentacaoDeEstoque>.Falha("SALDO_INSUFICIENTE");
            }

            var resultadoMovimentacao = MovimentacaoDeEstoque.Criar(
                IdProduto,
                tipo,
                quantidade,
                novoSaldo,
                origem);

            if (resultadoMovimentacao.EhFalha)
                return Resultado<MovimentacaoDeEstoque>.Falha(resultadoMovimentacao.Erros!);

            QuantidadeAtual = novoSaldo;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new SaldoDeEstoqueMovimentado(
                Id,
                IdProduto,
                tipo,
                sentido,
                quantidade.Valor,
                novoSaldo));

            return resultadoMovimentacao;
        }

        public static SaldoDeEstoque Reconstituir(
            Id idProduto,
            decimal quantidadeAtual,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new SaldoDeEstoque(
                idProduto,
                quantidadeAtual,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
