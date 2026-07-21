using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Estoque
{
    /// <summary>
    /// Mapeamento do agregado SaldoDeEstoque na tabela "estoque.saldos". Existe no
    /// máximo um saldo por produto — daí o índice único em id_produto, que é a
    /// própria garantia da invariante "um saldo por produto" no nível do banco.
    /// </summary>
    public sealed class SaldoDeEstoqueConfiguracao : ConfiguracaoDeEntidadeBase<SaldoDeEstoque>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<SaldoDeEstoque> builder)
        {
            builder.ToTable("saldos", Esquemas.Estoque);

            builder
                .Property(saldo => saldo.IdProduto)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_produto")
                .IsRequired();

            builder
                .Property(saldo => saldo.QuantidadeAtual)
                .HasColumnName("quantidade_atual")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder
                .HasIndex(saldo => saldo.IdProduto)
                .IsUnique()
                .HasDatabaseName("ux_saldos_id_produto");
        }
    }
}
