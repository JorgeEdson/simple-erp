using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Producao.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) dos conversores das coleções de Produção e
    /// Composição — a ida e volta lista de VOs ↔ jsonb.
    /// </summary>
    public sealed class ConversoresDeProducaoTests
    {
        private static NecessidadeDeMateriaPrima Necessidade(long idInsumo, decimal quantidade) =>
            NecessidadeDeMateriaPrima.TentarCriar(
                Id.TentarCriar(idInsumo).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia).Instancia!;

        private static ItemDeComposicao ItemComposicao(long idInsumo, decimal quantidade) =>
            ItemDeComposicao.TentarCriar(
                Id.TentarCriar(idInsumo).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia).Instancia!;

        [Fact]
        public void NecessidadesParaJson_DevePreservarInsumoEQuantidade()
        {
            var necessidades = new List<NecessidadeDeMateriaPrima>
            {
                Necessidade(202607210010, 2m),
                Necessidade(202607210011, 5.25m)
            };

            var paraBanco = ConversoresDeProducao.NecessidadesParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeProducao.NecessidadesParaJson.ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(necessidades));

            rehidratado.Should().HaveCount(2);
            rehidratado[1].IdInsumo.Should().Be(202607210011);
            rehidratado[1].QuantidadeNecessaria.Should().Be(5.25m);
        }

        [Fact]
        public void ItensDeComposicaoParaJson_DevePreservarInsumoEQuantidadePorUnidade()
        {
            var itens = new List<ItemDeComposicao>
            {
                ItemComposicao(202607210010, 2m),
                ItemComposicao(202607210011, 0.75m)
            };

            var paraBanco = ConversoresDeProducao.ItensDeComposicaoParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeProducao.ItensDeComposicaoParaJson.ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(itens));

            rehidratado.Should().HaveCount(2);
            rehidratado[1].IdInsumo.Should().Be(202607210011);
            rehidratado[1].QuantidadePorUnidade.Should().Be(0.75m);
        }

        [Fact]
        public void Comparadores_DevemIgualarColecoesComMesmoConteudo()
        {
            var necessidadesA = new List<NecessidadeDeMateriaPrima> { Necessidade(202607210010, 2m) };
            var necessidadesB = new List<NecessidadeDeMateriaPrima> { Necessidade(202607210010, 2m) };
            ConversoresDeProducao.ComparadorDeNecessidades.Equals(necessidadesA, necessidadesB)
                .Should().BeTrue();

            var itensA = new List<ItemDeComposicao> { ItemComposicao(202607210010, 2m) };
            var itensB = new List<ItemDeComposicao> { ItemComposicao(202607210010, 2m) };
            ConversoresDeProducao.ComparadorDeItensDeComposicao.Equals(itensA, itensB)
                .Should().BeTrue();
        }
    }
}
