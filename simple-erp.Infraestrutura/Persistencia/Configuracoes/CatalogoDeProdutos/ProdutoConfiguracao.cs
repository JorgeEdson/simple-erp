using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.CatalogoDeProdutos
{
    /// <summary>
    /// Mapeamento do agregado Produto na tabela "catalogo.produtos". Herda de
    /// ConfiguracaoDeEntidadeBase a chave (VO Id), as datas de auditoria e o Ignore
    /// dos eventos de domínio; aqui ficam apenas as colunas próprias do Produto.
    /// </summary>
    public sealed class ProdutoConfiguracao : ConfiguracaoDeEntidadeBase<Produto>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<Produto> builder)
        {
            builder.ToTable("produtos", Esquemas.CatalogoDeProdutos);

            builder
                .Property(produto => produto.Codigo)
                .HasConversion(ConversoresDeCatalogoDeProdutos.CodigoParaString)
                .HasColumnName("codigo")
                .HasMaxLength(50)
                .IsRequired();

            builder
                .Property(produto => produto.Descricao)
                .HasConversion(ConversoresDeCatalogoDeProdutos.DescricaoParaString)
                .HasColumnName("descricao")
                .HasMaxLength(200)
                .IsRequired();

            builder
                .Property(produto => produto.UnidadeDeMedida)
                .HasConversion(ConversoresDeCatalogoDeProdutos.UnidadeDeMedidaParaString)
                .HasColumnName("unidade_de_medida")
                .HasMaxLength(10)
                .IsRequired();

            // Enum persistido como int (padrão do EF) — estável a renomeações do C#.
            builder
                .Property(produto => produto.Classificacao)
                .HasConversion<int>()
                .HasColumnName("classificacao")
                .IsRequired();

            builder
                .Property(produto => produto.Ativo)
                .HasColumnName("ativo")
                .IsRequired();

            // Código é a chave natural do produto: único no catálogo.
            builder
                .HasIndex(produto => produto.Codigo)
                .IsUnique()
                .HasDatabaseName("ux_produtos_codigo");
        }
    }
}
