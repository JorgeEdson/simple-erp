using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Vendas
{
    /// <summary>
    /// Mapeamento do agregado PedidoDeVenda na tabela "vendas.pedidos_de_venda". Os
    /// itens (coleção de VOs) ficam em jsonb na própria linha, mapeados pelo campo de
    /// apoio "_itens"; a propriedade pública Itens e o total (ValorTotal, calculado a
    /// partir dos itens e do desconto do pedido) ficam ignorados.
    /// </summary>
    public sealed class PedidoDeVendaConfiguracao : ConfiguracaoDeEntidadeBase<PedidoDeVenda>
    {
        protected override void ConfigurarEntidade(EntityTypeBuilder<PedidoDeVenda> builder)
        {
            builder.ToTable("pedidos_de_venda", Esquemas.Vendas);

            builder
                .Property(pedido => pedido.Numero)
                .HasColumnName("numero")
                .IsRequired();

            builder
                .Property(pedido => pedido.IdCliente)
                .HasConversion(
                    ConversoresDeObjetosDeValor.IdParaLong,
                    ConversoresDeObjetosDeValor.ComparadorDeId)
                .HasColumnName("id_cliente")
                .IsRequired();

            builder
                .Property(pedido => pedido.Status)
                .HasConversion<int>()
                .HasColumnName("status")
                .IsRequired();

            builder
                .Property(pedido => pedido.DescontoDoPedido)
                .HasColumnName("desconto_do_pedido")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            builder
                .Property(pedido => pedido.MotivoCancelamento)
                .HasColumnName("motivo_cancelamento")
                .HasMaxLength(500);

            var itens = builder
                .Property<List<ItemDePedidoDeVenda>>("_itens")
                .HasConversion(ConversoresDeVendas.ItensParaJson, ConversoresDeVendas.ComparadorDeItens)
                .HasColumnName("itens")
                .HasColumnType("jsonb")
                .IsRequired();

            itens.Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(pedido => pedido.Itens);
            builder.Ignore(pedido => pedido.ValorTotal);

            // Número do pedido é sequencial e único.
            builder
                .HasIndex(pedido => pedido.Numero)
                .IsUnique()
                .HasDatabaseName("ux_pedidos_de_venda_numero");

            // Consultas: por cliente e por data do pedido.
            builder
                .HasIndex(pedido => new { pedido.IdCliente, pedido.DataCriacaoUtc })
                .HasDatabaseName("ix_pedidos_de_venda_cliente_data");
        }
    }
}
