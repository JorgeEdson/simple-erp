using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Eventos;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System.Linq;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class SaldoDeEstoqueTests
    {
        private static Quantidade Qtd(decimal valor) => Quantidade.TentarCriar(valor).Instancia;

        private static OrigemDaMovimentacao Origem(
            TipoOrigemMovimentacao tipo = TipoOrigemMovimentacao.AjusteManual,
            Guid? idReferencia = null) =>
            OrigemDaMovimentacao.TentarCriar(tipo, idReferencia).Instancia;

        [Fact]
        public void Criar_DeveIniciarComSaldoZeroEEmitirEvento()
        {
            var idProduto = new Guid("00000000-0000-0000-0000-202604020001");

            var resultado = SaldoDeEstoque.Criar(idProduto);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.QuantidadeAtual.Should().Be(0m);
            resultado.Instancia.EventosDeDominio
                .OfType<SaldoDeEstoqueCriado>()
                .Should().ContainSingle();
        }

        [Fact]
        public void Movimentar_EntradaPorCompra_DeveAumentarSaldoERetornarMovimentacao()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(10m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.EntradaPorCompra,
                Qtd(5m),
                Origem(TipoOrigemMovimentacao.Compra, new Guid("00000000-0000-0000-0000-000000000500")));

            resultado.EhSucesso.Should().BeTrue();
            saldo.QuantidadeAtual.Should().Be(15m);
            resultado.Instancia.SaldoResultante.Should().Be(15m);
            resultado.Instancia.EhEntrada.Should().BeTrue();
            saldo.EventosDeDominio.OfType<SaldoDeEstoqueMovimentado>().Should().ContainSingle();
        }

        [Fact]
        public void Movimentar_SaidaPorVenda_DeveReduzirSaldo_QuandoHouverSaldo()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(10m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.SaidaPorVenda,
                Qtd(4m),
                Origem(TipoOrigemMovimentacao.Venda, new Guid("00000000-0000-0000-0000-000000000700")));

            resultado.EhSucesso.Should().BeTrue();
            saldo.QuantidadeAtual.Should().Be(6m);
            resultado.Instancia.SaldoResultante.Should().Be(6m);
            resultado.Instancia.EhSaida.Should().BeTrue();
        }

        [Fact]
        public void Movimentar_Saida_DeveFalhar_QuandoNaoHouverSaldoSuficiente()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(3m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.SaidaPorVenda,
                Qtd(5m),
                Origem(TipoOrigemMovimentacao.Venda));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SALDO_INSUFICIENTE");
            saldo.QuantidadeAtual.Should().Be(3m); // saldo inalterado
        }

        [Fact]
        public void Movimentar_Saida_DevePermitirSaldoNegativo_QuandoConfigurado()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(3m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.SaidaPorProducao,
                Qtd(5m),
                Origem(TipoOrigemMovimentacao.Producao, new Guid("00000000-0000-0000-0000-000000000900")),
                permitirSaldoNegativo: true);

            resultado.EhSucesso.Should().BeTrue();
            saldo.QuantidadeAtual.Should().Be(-2m);
            saldo.EstaNegativo.Should().BeTrue();
            resultado.Instancia.SaldoResultante.Should().Be(-2m);
        }

        [Fact]
        public void Movimentar_EntradaPorProducao_DeveAumentarSaldo()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(0m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.EntradaPorProducao,
                Qtd(8m),
                Origem(TipoOrigemMovimentacao.Producao, new Guid("00000000-0000-0000-0000-000000000901")));

            resultado.EhSucesso.Should().BeTrue();
            saldo.QuantidadeAtual.Should().Be(8m);
        }

        [Fact]
        public void Movimentar_AjusteNegativo_DeveReduzirSaldo()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(10m).Criar();

            var resultado = saldo.Movimentar(
                TipoDeMovimentacao.AjusteNegativo,
                Qtd(2m),
                Origem(TipoOrigemMovimentacao.AjusteManual));

            resultado.EhSucesso.Should().BeTrue();
            saldo.QuantidadeAtual.Should().Be(8m);
            resultado.Instancia.Tipo.Should().Be(TipoDeMovimentacao.AjusteNegativo);
            resultado.Instancia.Sentido.Should().Be(SentidoDaMovimentacao.Saida);
        }
    }
}
