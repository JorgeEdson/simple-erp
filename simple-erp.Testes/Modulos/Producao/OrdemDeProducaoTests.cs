using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Eventos;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System.Linq;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class OrdemDeProducaoTests
    {
        private static NecessidadeDeMateriaPrima Necessidade(long idInsumo, decimal quantidade) =>
            NecessidadeDeMateriaPrima.TentarCriar(
                Id.TentarCriar(idInsumo).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia).Instancia;

        [Fact]
        public void Criar_DeveNascerCriadaEEmitirEvento()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;
            var idComposicao = Id.TentarCriar(202604020300).Instancia;
            var quantidade = Quantidade.TentarCriar(5m).Instancia;

            var resultado = OrdemDeProducao.Criar(
                idProduto, idComposicao, quantidade, new[] { Necessidade(202604020010, 10m) });

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.EstaCriada.Should().BeTrue();
            resultado.Instancia.EventosDeDominio.OfType<OrdemDeProducaoCriada>().Should().ContainSingle();
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoNaoHouverNecessidades()
        {
            var idProduto = Id.TentarCriar(202604020001).Instancia;
            var idComposicao = Id.TentarCriar(202604020300).Instancia;
            var quantidade = Quantidade.TentarCriar(5m).Instancia;

            var resultado = OrdemDeProducao.Criar(
                idProduto, idComposicao, quantidade, System.Array.Empty<NecessidadeDeMateriaPrima>());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("NECESSIDADES_OBRIGATORIAS");
        }

        [Fact]
        public void Confirmar_DeveTransicionarDeCriadaParaConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo().Criar();

            var resultado = ordem.Confirmar();

            resultado.EhSucesso.Should().BeTrue();
            ordem.EstaConfirmada.Should().BeTrue();
            ordem.EventosDeDominio.OfType<OrdemDeProducaoConfirmada>().Should().ContainSingle();
        }

        [Fact]
        public void Concluir_DeveFalhar_QuandoOrdemNaoEstiverConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo().Criar(); // criada

            var resultado = ordem.Concluir();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");
        }

        [Fact]
        public void Concluir_DeveEmitirEventoComInsumos_QuandoConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo()
                .SemNecessidades()
                .ComNecessidade(202604020010, 10m)
                .Confirmada()
                .Criar();

            var resultado = ordem.Concluir();

            resultado.EhSucesso.Should().BeTrue();
            ordem.EstaConcluida.Should().BeTrue();

            var evento = ordem.EventosDeDominio.OfType<OrdemDeProducaoConcluida>().Should().ContainSingle().Subject;
            evento.InsumosConsumidos.Should().ContainSingle(i => i.IdInsumo == 202604020010 && i.Quantidade == 10m);
        }

        [Fact]
        public void Cancelar_DeveFalhar_QuandoOrdemJaConcluida()
        {
            var ordem = OrdemDeProducaoBuilder.Novo().Concluida().Criar();

            var resultado = ordem.Cancelar();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORDEM_DE_PRODUCAO_CONCLUIDA_NAO_PODE_SER_CANCELADA");
        }

        [Fact]
        public void Cancelar_DevePermitir_QuandoConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo().Confirmada().Criar();

            var resultado = ordem.Cancelar();

            resultado.EhSucesso.Should().BeTrue();
            ordem.EstaCancelada.Should().BeTrue();
        }
    }
}
