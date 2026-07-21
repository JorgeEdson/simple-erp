using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Suprimentos.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) do conversor da coleção de itens ↔ jsonb.
    /// </summary>
    public sealed class ConversoresDeSuprimentosTests
    {
        private static ItemDePedidoDeCompra Item(long idProduto, decimal quantidade, decimal custo) =>
            ItemDePedidoDeCompra.TentarCriar(
                Id.TentarCriar(idProduto).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia,
                Dinheiro.TentarCriar(custo).Instancia).Instancia!;

        [Fact]
        public void ItensParaJson_DevePreservarIdQuantidadeECusto()
        {
            var itens = new List<ItemDePedidoDeCompra>
            {
                Item(202607210010, 5m, 2.50m),
                Item(202607210011, 3m, 4.00m)
            };

            var paraBanco = ConversoresDeSuprimentos.ItensParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeSuprimentos.ItensParaJson.ConvertFromProviderExpression.Compile();

            var json = paraBanco(itens);
            json.Should().Contain("IdProduto");

            var rehidratado = doBanco(json);
            rehidratado.Should().HaveCount(2);
            rehidratado[0].IdProduto.Should().Be(202607210010);
            rehidratado[0].Quantidade.Should().Be(5m);
            rehidratado[0].CustoUnitario.Should().Be(2.50m);
            rehidratado[1].Subtotal.Should().Be(12.00m, "3 * 4.00");
        }

        [Fact]
        public void ItensParaJson_DeveSuportarColecaoVazia()
        {
            var paraBanco = ConversoresDeSuprimentos.ItensParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeSuprimentos.ItensParaJson.ConvertFromProviderExpression.Compile();

            doBanco(paraBanco(new List<ItemDePedidoDeCompra>())).Should().BeEmpty();
        }

        [Fact]
        public void ComparadorDeItens_DeveIgualarColecoesComMesmoConteudo()
        {
            var a = new List<ItemDePedidoDeCompra> { Item(202607210010, 5m, 2.50m) };
            var b = new List<ItemDePedidoDeCompra> { Item(202607210010, 5m, 2.50m) };

            ConversoresDeSuprimentos.ComparadorDeItens.Equals(a, b).Should().BeTrue();
        }
    }
}
