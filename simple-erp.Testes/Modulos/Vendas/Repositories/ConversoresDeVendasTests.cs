using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Vendas.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) do conversor da coleção de itens de venda ↔ jsonb.
    /// </summary>
    public sealed class ConversoresDeVendasTests
    {
        private static ItemDePedidoDeVenda Item(long idProduto, decimal quantidade, decimal preco, decimal desconto) =>
            ItemDePedidoDeVenda.TentarCriar(
                Id.TentarCriar(idProduto).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia,
                Dinheiro.TentarCriar(preco).Instancia,
                Dinheiro.TentarCriar(desconto).Instancia).Instancia!;

        [Fact]
        public void ItensParaJson_DevePreservarTodosOsCamposDoItem()
        {
            var itens = new List<ItemDePedidoDeVenda>
            {
                Item(202607210010, 4m, 10m, 0m),
                Item(202607210011, 2m, 25m, 5m)
            };

            var paraBanco = ConversoresDeVendas.ItensParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeVendas.ItensParaJson.ConvertFromProviderExpression.Compile();

            var json = paraBanco(itens);
            json.Should().Contain("PrecoUnitario");

            var rehidratado = doBanco(json);
            rehidratado.Should().HaveCount(2);
            rehidratado[1].IdProduto.Should().Be(202607210011);
            rehidratado[1].Quantidade.Should().Be(2m);
            rehidratado[1].PrecoUnitario.Should().Be(25m);
            rehidratado[1].Desconto.Should().Be(5m);
            // Subtotal = 2*25 - 5 = 45
            rehidratado[1].Subtotal.Should().Be(45m);
        }

        [Fact]
        public void ItensParaJson_DeveSuportarColecaoVazia()
        {
            var paraBanco = ConversoresDeVendas.ItensParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeVendas.ItensParaJson.ConvertFromProviderExpression.Compile();

            doBanco(paraBanco(new List<ItemDePedidoDeVenda>())).Should().BeEmpty();
        }

        [Fact]
        public void ComparadorDeItens_DeveIgualarColecoesComMesmoConteudo()
        {
            var a = new List<ItemDePedidoDeVenda> { Item(202607210010, 4m, 10m, 0m) };
            var b = new List<ItemDePedidoDeVenda> { Item(202607210010, 4m, 10m, 0m) };

            ConversoresDeVendas.ComparadorDeItens.Equals(a, b).Should().BeTrue();
        }
    }
}
