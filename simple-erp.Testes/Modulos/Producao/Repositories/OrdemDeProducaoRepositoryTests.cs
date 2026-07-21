using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Infraestrutura.Repositorios.Producao;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class OrdemDeProducaoRepositoryTests
        : IClassFixture<PostgresProducaoFixture>, IAsyncLifetime
    {
        private const long IdProdutoFabricado = 202607210050;

        private readonly PostgresProducaoFixture _fixture;

        public OrdemDeProducaoRepositoryTests(PostgresProducaoFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        private async Task SalvarAsync(params OrdemDeProducao[] ordens)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new OrdemDeProducaoRepository(contexto);

            foreach (var ordem in ordens)
                (await repositorio.AdicionarAsync(ordem)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirAsNecessidadesEmJsonb()
        {
            var ordem = OrdemDeProducaoBuilder.Novo()
                .ComId(202607210900)
                .ComIdProdutoFabricado(IdProdutoFabricado)
                .ComQuantidadeAProduzir(10m)
                .SemNecessidades()
                .ComNecessidade(202607210010, 2m)
                .ComNecessidade(202607210011, 5m)
                .Criar();

            await SalvarAsync(ordem);

            await using var contexto = _fixture.CriarContexto();
            var recuperada = (await new OrdemDeProducaoRepository(contexto)
                .ObterPorIdAsync(IdDe(202607210900))).Instancia;

            recuperada.Should().NotBeNull();
            recuperada!.IdProdutoFabricado.Valor.Should().Be(IdProdutoFabricado);
            recuperada.QuantidadeAProduzir.Should().Be(10m);
            recuperada.Status.Should().Be(StatusOrdemDeProducao.Criada);
            recuperada.Necessidades.Should().HaveCount(2);
            recuperada.Necessidades.Single(n => n.IdInsumo == 202607210011)
                .QuantidadeNecessaria.Should().Be(5m);
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new OrdemDeProducaoRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(IdDe(999999999999));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
            (await repositorio.ExistePorIdAsync(IdDe(999999999999))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task TransicaoDeStatus_ViaEntidadeRastreada_DevePersistir()
        {
            await SalvarAsync(OrdemDeProducaoBuilder.Novo()
                .ComId(202607210910).ComIdProdutoFabricado(IdProdutoFabricado)
                .SemNecessidades().ComNecessidade(202607210010, 1m)
                .Criar());

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new OrdemDeProducaoRepository(contexto);
                var ordem = (await repositorio.ObterPorIdAsync(IdDe(202607210910))).Instancia!;

                ordem.Confirmar().EhSucesso.Should().BeTrue();
                ordem.Concluir().EhSucesso.Should().BeTrue();

                await repositorio.AtualizarAsync(ordem);
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperada = (await new OrdemDeProducaoRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607210910))).Instancia!;

            recuperada.Status.Should().Be(StatusOrdemDeProducao.Concluida);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorProdutoEStatus()
        {
            await SalvarAsync(
                OrdemDeProducaoBuilder.Novo().ComId(202607210920).ComIdProdutoFabricado(IdProdutoFabricado)
                    .SemNecessidades().ComNecessidade(202607210010, 1m).Criar(),
                OrdemDeProducaoBuilder.Novo().ComId(202607210921).ComIdProdutoFabricado(IdProdutoFabricado)
                    .SemNecessidades().ComNecessidade(202607210010, 1m).Confirmada().Criar(),
                OrdemDeProducaoBuilder.Novo().ComId(202607210922).ComIdProdutoFabricado(202607219999)
                    .SemNecessidades().ComNecessidade(202607210010, 1m).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new OrdemDeProducaoRepository(contexto);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarOrdensDeProducaoFiltros(IdProdutoFabricado: IdProdutoFabricado))).Instancia
                .TotalRegistros.Should().Be(2);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarOrdensDeProducaoFiltros(Status: StatusOrdemDeProducao.Confirmada))).Instancia
                .TotalRegistros.Should().Be(1);
        }
    }
}
