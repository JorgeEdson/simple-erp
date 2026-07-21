using FluentAssertions;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Testes.Modulos.Estoque.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) do conversor de OrigemDaMovimentacao ↔ jsonb.
    /// </summary>
    public sealed class ConversoresDeEstoqueTests
    {
        [Fact]
        public void OrigemParaJson_DeveSerializarEDesserializarComReferencia()
        {
            var original = OrigemDaMovimentacao.TentarCriar(
                TipoOrigemMovimentacao.Compra, idReferencia: 12345).Instancia!;

            var paraBanco = ConversoresDeEstoque.OrigemParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeEstoque.OrigemParaJson.ConvertFromProviderExpression.Compile();

            var json = paraBanco(original);
            json.Should().Contain("\"Tipo\"");
            json.Should().Contain("\"IdReferencia\"");

            var rehidratado = doBanco(json);
            rehidratado.Tipo.Should().Be(TipoOrigemMovimentacao.Compra);
            rehidratado.IdReferencia.Should().Be(12345);
        }

        [Fact]
        public void OrigemParaJson_DeveSuportarReferenciaNula()
        {
            var original = OrigemDaMovimentacao.TentarCriar(
                TipoOrigemMovimentacao.AjusteManual).Instancia!;

            var paraBanco = ConversoresDeEstoque.OrigemParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeEstoque.OrigemParaJson.ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(original));

            rehidratado.Tipo.Should().Be(TipoOrigemMovimentacao.AjusteManual);
            rehidratado.IdReferencia.Should().BeNull();
        }
    }
}
