using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.Eventos;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System.Linq;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class ComposicaoDeProdutoTests
    {
        private static ItemDeComposicao Item(long idInsumo, decimal quantidade) =>
            ItemDeComposicao.TentarCriar(
                Id.TentarCriar(idInsumo).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia).Instancia;

        [Fact]
        public void Criar_DeveNascerInativaEEmitirEvento()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;

            var resultado = ComposicaoDeProduto.Criar(idProduto, 1, new[] { Item(202604020010, 2m) });

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Ativa.Should().BeFalse();
            resultado.Instancia.Versao.Should().Be(1);
            resultado.Instancia.EventosDeDominio.OfType<ComposicaoDeProdutoCriada>().Should().ContainSingle();
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoNaoHouverItens()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;

            var resultado = ComposicaoDeProduto.Criar(idProduto, 1, System.Array.Empty<ItemDeComposicao>());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("COMPOSICAO_SEM_ITENS");
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoHouverInsumoRepetido()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;

            var resultado = ComposicaoDeProduto.Criar(idProduto, 1, new[]
            {
                Item(202604020010, 2m),
                Item(202604020010, 5m)
            });

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("INSUMO_REPETIDO_NA_COMPOSICAO");
        }

        [Fact]
        public void Ativar_DeveTornarAtivaEEmitirEvento()
        {
            var composicao = ComposicaoDeProdutoBuilder.Novo().Criar();

            var resultado = composicao.Ativar();

            resultado.EhSucesso.Should().BeTrue();
            composicao.Ativa.Should().BeTrue();
            composicao.EventosDeDominio.OfType<ComposicaoDeProdutoAtivada>().Should().ContainSingle();
        }

        [Fact]
        public void CalcularNecessidades_DeveFalhar_QuandoComposicaoInativa()
        {
            var composicao = ComposicaoDeProdutoBuilder.Novo().Criar(); // inativa

            var resultado = composicao.CalcularNecessidades(Quantidade.TentarCriar(5m).Instancia);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("COMPOSICAO_NAO_ESTA_ATIVA");
        }

        [Fact]
        public void CalcularNecessidades_DeveMultiplicarPelaQuantidade_QuandoAtiva()
        {
            var composicao = ComposicaoDeProdutoBuilder.Novo()
                .SemItens()
                .ComItem(202604020010, 2m)
                .ComItem(202604020011, 3m)
                .Ativa()
                .Criar();

            var resultado = composicao.CalcularNecessidades(Quantidade.TentarCriar(5m).Instancia);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().ContainSingle(n => n.IdInsumo == 202604020010 && n.QuantidadeTotal == 10m);
            resultado.Instancia.Should().ContainSingle(n => n.IdInsumo == 202604020011 && n.QuantidadeTotal == 15m);
        }
    }
}
