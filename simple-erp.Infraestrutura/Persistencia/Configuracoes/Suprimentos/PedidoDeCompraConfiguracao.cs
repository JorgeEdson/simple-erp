using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Suprimentos
{
    /// <summary>
    /// Mapeamento do agregado PedidoDeCompra na tabela "suprimentos.pedidos_de_compra".
    /// Os itens (coleção de VOs) ficam em jsonb na própria linha do pedido, mapeados
    /// pelo campo de apoio "_itens" — a propriedade pública Itens é somente-leitura e
    /// fica ignorada, assim como o total (ValorTotal), que é calculado a partir dos itens.
    /// </summary>
    public sealed class PedidoDeCompraConfiguracao : ConfiguracaoDeEntidadeBase<PedidoDeCompra>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<PedidoDeCompra> builder)
        {
            builder.ToTable("pedidos_de_compra", Esquemas.Suprimentos);

            builder
                .Property(pedido => pedido.IdFornecedor)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_fornecedor")
                .IsRequired();

            builder
                .Property(pedido => pedido.Status)
                .HasConversion<int>()
                .HasColumnName("status")
                .IsRequired();

            // Itens → jsonb, mapeados pelo campo de apoio.
            var itens = builder
                .Property<List<ItemDePedidoDeCompra>>("_itens")
                .HasConversion(ConversoresDeSuprimentos.ItensParaJson, ConversoresDeSuprimentos.ComparadorDeItens)
                .HasColumnName("itens")
                .HasColumnType("jsonb")
                .IsRequired();

            itens.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

            // Propriedades derivadas — não são colunas.
            builder.Ignore(pedido => pedido.Itens);
            builder.Ignore(pedido => pedido.ValorTotal);

            // Consultas: por fornecedor e por data do pedido (data de criação).
            builder
                .HasIndex(pedido => new { pedido.IdFornecedor, pedido.DataCriacaoUtc })
                .HasDatabaseName("ix_pedidos_de_compra_fornecedor_data");
        }
    }
}
