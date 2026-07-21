using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Producao
{
    /// <summary>
    /// Mapeamento do agregado ComposicaoDeProduto (subdomínio Composição) na tabela
    /// "producao.composicoes_de_produto". Cada linha é uma versão da receita; os itens
    /// (coleção de VOs) ficam em jsonb, mapeados pelo campo de apoio "_itens".
    ///
    /// A unicidade (produto + versão) é garantida no banco por índice único — é o que
    /// dá consistência ao "próximo número de versão" e impede versões duplicadas.
    /// (A regra "apenas uma versão ativa por produto" é orquestrada no domínio, via
    /// o handler de unicidade de receita ativa.)
    /// </summary>
    public sealed class ComposicaoDeProdutoConfiguracao : ConfiguracaoDeEntidadeBase<ComposicaoDeProduto>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<ComposicaoDeProduto> builder)
        {
            builder.ToTable("composicoes_de_produto", Esquemas.Producao);

            builder
                .Property(composicao => composicao.IdProdutoFabricado)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_produto_fabricado")
                .IsRequired();

            builder
                .Property(composicao => composicao.Versao)
                .HasColumnName("versao")
                .IsRequired();

            builder
                .Property(composicao => composicao.Ativa)
                .HasColumnName("ativa")
                .IsRequired();

            var itens = builder
                .Property<List<ItemDeComposicao>>("_itens")
                .HasConversion(
                    ConversoresDeProducao.ItensDeComposicaoParaJson,
                    ConversoresDeProducao.ComparadorDeItensDeComposicao)
                .HasColumnName("itens")
                .HasColumnType("jsonb")
                .IsRequired();

            itens.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(composicao => composicao.Itens);

            builder
                .HasIndex(composicao => new { composicao.IdProdutoFabricado, composicao.Versao })
                .IsUnique()
                .HasDatabaseName("ux_composicoes_produto_versao");
        }
    }
}
