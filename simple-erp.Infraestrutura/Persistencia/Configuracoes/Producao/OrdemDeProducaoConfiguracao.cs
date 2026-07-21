using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Producao
{
    /// <summary>
    /// Mapeamento do agregado OrdemDeProducao na tabela "producao.ordens_de_producao".
    /// As necessidades de matéria-prima (coleção de VOs) ficam em jsonb na própria linha,
    /// mapeadas pelo campo de apoio "_necessidades"; a propriedade pública Necessidades
    /// é somente-leitura e fica ignorada.
    /// </summary>
    public sealed class OrdemDeProducaoConfiguracao : ConfiguracaoDeEntidadeBase<OrdemDeProducao>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<OrdemDeProducao> builder)
        {
            builder.ToTable("ordens_de_producao", Esquemas.Producao);

            builder
                .Property(ordem => ordem.IdProdutoFabricado)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_produto_fabricado")
                .IsRequired();

            builder
                .Property(ordem => ordem.IdComposicao)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_composicao")
                .IsRequired();

            builder
                .Property(ordem => ordem.QuantidadeAProduzir)
                .HasColumnName("quantidade_a_produzir")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder
                .Property(ordem => ordem.Status)
                .HasConversion<int>()
                .HasColumnName("status")
                .IsRequired();

            var necessidades = builder
                .Property<List<NecessidadeDeMateriaPrima>>("_necessidades")
                .HasConversion(
                    ConversoresDeProducao.NecessidadesParaJson,
                    ConversoresDeProducao.ComparadorDeNecessidades)
                .HasColumnName("necessidades")
                .HasColumnType("jsonb")
                .IsRequired();

            necessidades.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(ordem => ordem.Necessidades);

            builder
                .HasIndex(ordem => new { ordem.IdProdutoFabricado, ordem.DataCriacaoUtc })
                .HasDatabaseName("ix_ordens_de_producao_produto_data");
        }
    }
}
