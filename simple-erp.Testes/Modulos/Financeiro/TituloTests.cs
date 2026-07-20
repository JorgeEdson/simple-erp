using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.Eventos;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System;
using System.Linq;

namespace simple_erp.Testes.Modulos.Financeiro
{
    public sealed class TituloTests
    {
        private static Dinheiro Valor(decimal v) => Dinheiro.TentarCriar(v).Instancia;

        private static Titulo TituloAPagar(decimal valorOriginal = 100.00m) =>
            TituloBuilder.Novo().ComoAPagar().ComValorOriginal(valorOriginal).Criar();

        [Fact]
        public void Criar_DeveNascerEmAbertoEEmitirEvento()
        {
            var idParceiro = Id.TentarCriar(202604020002).Instancia;
            var origem = OrigemDoTitulo.TentarCriar(TipoOrigemTitulo.Compra, 10).Instancia;

            var resultado = Titulo.Criar(
                TipoDeTitulo.APagar, idParceiro, origem, Valor(100m), DateTime.UtcNow.AddDays(15));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.EstaEmAberto.Should().BeTrue();
            resultado.Instancia.SaldoDevedor.Should().Be(100m);
            resultado.Instancia.EventosDeDominio.OfType<TituloEmitido>().Should().ContainSingle();
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoVencimentoForNoPassado()
        {
            var idParceiro = Id.TentarCriar(202604020002).Instancia;
            var origem = OrigemDoTitulo.TentarCriar(TipoOrigemTitulo.Compra).Instancia;

            var resultado = Titulo.Criar(
                TipoDeTitulo.APagar, idParceiro, origem, Valor(100m), DateTime.UtcNow.AddDays(-5));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("VENCIMENTO_INVALIDO");
        }

        [Fact]
        public void Baixar_Parcialmente_DeveReduzirSaldoEMarcarComoParcial()
        {
            var titulo = TituloAPagar(100m);

            var resultado = titulo.Baixar(Valor(40m));

            resultado.EhSucesso.Should().BeTrue();
            titulo.ValorBaixado.Should().Be(40m);
            titulo.SaldoDevedor.Should().Be(60m);
            titulo.EstaParcialmenteBaixado.Should().BeTrue();
            titulo.EventosDeDominio.OfType<TituloBaixado>().Should().ContainSingle();
            titulo.EventosDeDominio.OfType<TituloLiquidado>().Should().BeEmpty();
        }

        [Fact]
        public void Baixar_TotalmenteEmParcelas_DeveLiquidarEEmitirEvento()
        {
            var titulo = TituloAPagar(100m);

            titulo.Baixar(Valor(60m));
            var resultado = titulo.Baixar(Valor(40m));

            resultado.EhSucesso.Should().BeTrue();
            titulo.SaldoDevedor.Should().Be(0m);
            titulo.EstaLiquidado.Should().BeTrue();
            titulo.Baixas.Should().HaveCount(2);
            titulo.EventosDeDominio.OfType<TituloLiquidado>().Should().ContainSingle();
        }

        [Fact]
        public void Baixar_DeveFalhar_QuandoValorExcederSaldoDevedor()
        {
            var titulo = TituloAPagar(100m);

            var resultado = titulo.Baixar(Valor(150m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("VALOR_BAIXA_EXCEDE_SALDO");
            titulo.ValorBaixado.Should().Be(0m);
        }

        [Fact]
        public void Baixar_DeveFalhar_QuandoTituloJaLiquidado()
        {
            var titulo = TituloBuilder.Novo().ComValorOriginal(100m).ComBaixaInicial(100m).Criar();

            titulo.EstaLiquidado.Should().BeTrue();

            var resultado = titulo.Baixar(Valor(10m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TITULO_JA_LIQUIDADO");
        }

        [Fact]
        public void Cancelar_DeveFalhar_QuandoTituloLiquidado()
        {
            var titulo = TituloBuilder.Novo().ComValorOriginal(100m).ComBaixaInicial(100m).Criar();

            var resultado = titulo.Cancelar();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("TITULO_LIQUIDADO_NAO_PODE_SER_CANCELADO");
        }

        [Fact]
        public void Cancelar_DevePermitir_QuandoParcialmenteBaixado()
        {
            var titulo = TituloBuilder.Novo().ComValorOriginal(100m).ComBaixaInicial(30m).Criar();

            var resultado = titulo.Cancelar();

            resultado.EhSucesso.Should().BeTrue();
            titulo.EstaCancelado.Should().BeTrue();
            titulo.EventosDeDominio.OfType<TituloCancelado>().Should().ContainSingle();
        }
    }
}
