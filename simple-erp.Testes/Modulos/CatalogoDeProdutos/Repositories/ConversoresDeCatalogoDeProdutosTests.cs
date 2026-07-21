using FluentAssertions;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) dos conversores de VO ↔ primitivo do Catálogo.
    /// </summary>
    public sealed class ConversoresDeCatalogoDeProdutosTests
    {
        [Fact]
        public void CodigoParaString_DevePersistirNormalizadoEReidratar()
        {
            // O VO normaliza para maiúsculas; o valor persistido é o canônico.
            var original = CodigoProduto.TentarCriar("prod-001").Instancia!;

            var paraBanco = ConversoresDeCatalogoDeProdutos.CodigoParaString
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeCatalogoDeProdutos.CodigoParaString
                .ConvertFromProviderExpression.Compile();

            var persistido = paraBanco(original);
            persistido.Should().Be("PROD-001");

            doBanco(persistido).Valor.Should().Be("PROD-001");
        }

        [Fact]
        public void DescricaoParaString_DeveIrEVoltarSemPerda()
        {
            var original = DescricaoProduto.TentarCriar("Parafuso sextavado M8").Instancia!;

            var paraBanco = ConversoresDeCatalogoDeProdutos.DescricaoParaString
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeCatalogoDeProdutos.DescricaoParaString
                .ConvertFromProviderExpression.Compile();

            doBanco(paraBanco(original)).Valor.Should().Be(original.Valor);
        }

        [Fact]
        public void UnidadeDeMedidaParaString_DeveIrEVoltarSemPerda()
        {
            var original = UnidadeDeMedida.TentarCriar("kg").Instancia!;

            var paraBanco = ConversoresDeCatalogoDeProdutos.UnidadeDeMedidaParaString
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeCatalogoDeProdutos.UnidadeDeMedidaParaString
                .ConvertFromProviderExpression.Compile();

            var persistido = paraBanco(original);
            persistido.Should().Be("KG");

            doBanco(persistido).Valor.Should().Be("KG");
        }
    }
}
