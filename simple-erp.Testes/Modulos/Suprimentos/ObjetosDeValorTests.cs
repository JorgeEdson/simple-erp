using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class ObjetosDeValorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.5)]
        public void Quantidade_DeveFalhar_QuandoNaoForPositiva(double valor)
        {
            var resultado = Quantidade.TentarCriar((decimal)valor);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("QUANTIDADE_DEVE_SER_POSITIVA");
        }

        [Fact]
        public void Quantidade_DeveCriar_QuandoValorForPositivo()
        {
            var resultado = Quantidade.TentarCriar(2.5m);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Valor.Should().Be(2.5m);
        }

        [Fact]
        public void Dinheiro_DeveFalhar_QuandoValorForNegativo()
        {
            var resultado = Dinheiro.TentarCriar(-0.01m);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("VALOR_MONETARIO_NEGATIVO");
        }

        [Fact]
        public void Dinheiro_DeveArredondarParaDuasCasasDecimais()
        {
            var resultado = Dinheiro.TentarCriar(10.005m);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Valor.Should().Be(10.01m);
        }

        [Fact]
        public void ItemDePedidoDeCompra_DeveCalcularSubtotal()
        {
            var item = ItemDePedidoDeCompra.TentarCriar(
                Id.TentarCriar(202604020001).Instancia,
                Quantidade.TentarCriar(4m).Instancia,
                Dinheiro.TentarCriar(2.50m).Instancia);

            item.EhSucesso.Should().BeTrue();
            item.Instancia.Subtotal.Should().Be(10.00m);
            item.Instancia.IdProduto.Should().Be(202604020001);
        }
    }
}
