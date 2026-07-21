using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Financeiro
{
    /// <summary>
    /// Mapeamento do agregado Titulo na tabela "financeiro.titulos". O histórico de
    /// baixas (coleção de VOs) é persistido como jsonb na própria linha do título,
    /// mapeado pelo campo de apoio "_baixas" — a propriedade pública Baixas é apenas
    /// leitura e fica ignorada. Assim o agregado é carregado e salvo como uma unidade,
    /// respeitando a regra "uma transação = um agregado".
    /// </summary>
    public sealed class TituloConfiguracao : ConfiguracaoDeEntidadeBase<Titulo>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<Titulo> builder)
        {
            builder.ToTable("titulos", Esquemas.Financeiro);

            builder
                .Property(titulo => titulo.Tipo)
                .HasConversion<int>()
                .HasColumnName("tipo")
                .IsRequired();

            builder
                .Property(titulo => titulo.IdParceiro)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_parceiro")
                .IsRequired();

            builder
                .Property(titulo => titulo.Origem)
                .HasConversion(ConversoresDeFinanceiro.OrigemParaJson)
                .HasColumnName("origem")
                .HasColumnType("jsonb")
                .IsRequired();

            builder
                .Property(titulo => titulo.ValorOriginal)
                .HasColumnName("valor_original")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder
                .Property(titulo => titulo.DataVencimentoUtc)
                .HasColumnName("data_vencimento_utc")
                .IsRequired();

            builder
                .Property(titulo => titulo.Status)
                .HasConversion<int>()
                .HasColumnName("status")
                .IsRequired();

            // Histórico de baixas → jsonb, mapeado pelo campo de apoio.
            var baixas = builder
                .Property<List<BaixaDoTitulo>>("_baixas")
                .HasConversion(ConversoresDeFinanceiro.BaixasParaJson, ConversoresDeFinanceiro.ComparadorDeBaixas)
                .HasColumnName("baixas")
                .HasColumnType("jsonb")
                .IsRequired();

            baixas.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

            // A propriedade pública Baixas é somente-leitura (deriva de _baixas):
            // não é uma coluna, então fica de fora do modelo relacional.
            builder.Ignore(titulo => titulo.Baixas);

            // Consultas do extrato: por parceiro e por vencimento.
            builder
                .HasIndex(titulo => new { titulo.IdParceiro, titulo.DataVencimentoUtc })
                .HasDatabaseName("ix_titulos_parceiro_vencimento");
        }
    }
}
