using FluentAssertions;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ObjetosDeValorEstoqueTests
    {
        [Theory]
        [InlineData(TipoDeMovimentacao.EntradaPorCompra, SentidoDaMovimentacao.Entrada)]
        [InlineData(TipoDeMovimentacao.EntradaPorProducao, SentidoDaMovimentacao.Entrada)]
        [InlineData(TipoDeMovimentacao.AjustePositivo, SentidoDaMovimentacao.Entrada)]
        [InlineData(TipoDeMovimentacao.SaidaPorVenda, SentidoDaMovimentacao.Saida)]
        [InlineData(TipoDeMovimentacao.SaidaPorProducao, SentidoDaMovimentacao.Saida)]
        [InlineData(TipoDeMovimentacao.AjusteNegativo, SentidoDaMovimentacao.Saida)]
        public void TiposDeMovimentacao_Sentido_DeveMapearCorretamente(
            TipoDeMovimentacao tipo,
            SentidoDaMovimentacao esperado)
        {
            TiposDeMovimentacao.Sentido(tipo).Should().Be(esperado);
        }

        [Fact]
        public void Quantidade_DeveFalhar_QuandoNaoForPositiva()
        {
            var resultado = Quantidade.TentarCriar(0m);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("QUANTIDADE_DEVE_SER_POSITIVA");
        }

        [Fact]
        public void OrigemDaMovimentacao_DeveFalhar_QuandoReferenciaForInvalida()
        {
            var resultado = OrigemDaMovimentacao.TentarCriar(TipoOrigemMovimentacao.Compra, idReferencia: 0);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORIGEM_REFERENCIA_INVALIDA");
        }

        [Fact]
        public void OrigemDaMovimentacao_DeveCriar_QuandoReferenciaForNula()
        {
            var resultado = OrigemDaMovimentacao.TentarCriar(TipoOrigemMovimentacao.AjusteManual);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Tipo.Should().Be(TipoOrigemMovimentacao.AjusteManual);
            resultado.Instancia.IdReferencia.Should().BeNull();
        }
    }
}
