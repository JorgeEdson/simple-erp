using FluentAssertions;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Financeiro.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco) dos conversores do Financeiro: a origem e,
    /// sobretudo, a coleção de baixas — a ida e volta lista de VOs ↔ jsonb.
    /// </summary>
    public sealed class ConversoresDeFinanceiroTests
    {
        [Fact]
        public void OrigemParaJson_DeveSerializarEDesserializar()
        {
            var original = OrigemDoTitulo.TentarCriar(TipoOrigemTitulo.Venda, 987).Instancia!;

            var paraBanco = ConversoresDeFinanceiro.OrigemParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeFinanceiro.OrigemParaJson.ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(original));
            rehidratado.Tipo.Should().Be(TipoOrigemTitulo.Venda);
            rehidratado.IdReferencia.Should().Be(987);
        }

        [Fact]
        public void BaixasParaJson_DevePreservarMontantesEDatasDaColecao()
        {
            var baixas = new List<BaixaDoTitulo>
            {
                BaixaDoTitulo.TentarCriar(Dinheiro.TentarCriar(50m).Instancia,
                    new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Instancia!,
                BaixaDoTitulo.TentarCriar(Dinheiro.TentarCriar(75.50m).Instancia,
                    new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc)).Instancia!
            };

            var paraBanco = ConversoresDeFinanceiro.BaixasParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeFinanceiro.BaixasParaJson.ConvertFromProviderExpression.Compile();

            var json = paraBanco(baixas);
            json.Should().Contain("Montante");

            var rehidratado = doBanco(json);
            rehidratado.Should().HaveCount(2);
            rehidratado[0].Montante.Should().Be(50m);
            rehidratado[1].Montante.Should().Be(75.50m);
            rehidratado[1].DataUtc.Should().Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void BaixasParaJson_DeveSuportarColecaoVazia()
        {
            var paraBanco = ConversoresDeFinanceiro.BaixasParaJson.ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeFinanceiro.BaixasParaJson.ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(new List<BaixaDoTitulo>()));

            rehidratado.Should().BeEmpty();
        }

        [Fact]
        public void ComparadorDeBaixas_DeveIgualarColecoesComMesmoConteudo()
        {
            List<BaixaDoTitulo> Criar() => new()
            {
                BaixaDoTitulo.TentarCriar(Dinheiro.TentarCriar(10m).Instancia,
                    new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Instancia!
            };

            var a = Criar();
            var b = Criar();

            ConversoresDeFinanceiro.ComparadorDeBaixas.Equals(a, b).Should().BeTrue();
        }
    }
}
