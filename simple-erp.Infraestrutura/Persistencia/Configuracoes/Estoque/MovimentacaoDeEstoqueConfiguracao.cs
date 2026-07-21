using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Estoque
{
    /// <summary>
    /// Mapeamento do agregado MovimentacaoDeEstoque na tabela "estoque.movimentacoes".
    /// É um registro de extrato imutável (append-only): cada entrada/saída/ajuste gera
    /// uma linha, com o saldo resultante congelado no momento da operação. A origem
    /// (documento que deu causa) fica em jsonb, permitindo o filtro por tipo de origem
    /// no extrato paginado.
    /// </summary>
    public sealed class MovimentacaoDeEstoqueConfiguracao : ConfiguracaoDeEntidadeBase<MovimentacaoDeEstoque>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<MovimentacaoDeEstoque> builder)
        {
            builder.ToTable("movimentacoes", Esquemas.Estoque);

            builder
                .Property(mov => mov.IdProduto)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_produto")
                .IsRequired();

            builder
                .Property(mov => mov.Tipo)
                .HasConversion<int>()
                .HasColumnName("tipo")
                .IsRequired();

            builder
                .Property(mov => mov.Sentido)
                .HasConversion<int>()
                .HasColumnName("sentido")
                .IsRequired();

            builder
                .Property(mov => mov.Quantidade)
                .HasColumnName("quantidade")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder
                .Property(mov => mov.SaldoResultante)
                .HasColumnName("saldo_resultante")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder
                .Property(mov => mov.Origem)
                .HasConversion(ConversoresDeEstoque.OrigemParaJson)
                .HasColumnName("origem")
                .HasColumnType("jsonb")
                .IsRequired();

            builder
                .Property(mov => mov.DataMovimentacaoUtc)
                .HasColumnName("data_movimentacao_utc")
                .IsRequired();

            // Extrato é consultado por produto e por data — índice de apoio.
            builder
                .HasIndex(mov => new { mov.IdProduto, mov.DataMovimentacaoUtc })
                .HasDatabaseName("ix_movimentacoes_produto_data");
        }
    }
}
