using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Infraestrutura.Repositorios.Estoque;

namespace simple_erp.Testes.Modulos.Estoque.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class MovimentacaoDeEstoqueRepositoryTests
        : IClassFixture<PostgresEstoqueFixture>, IAsyncLifetime
    {
        private const long IdProdutoA = 202607210001;
        private const long IdProdutoB = 202607210002;

        private readonly PostgresEstoqueFixture _fixture;

        public MovimentacaoDeEstoqueRepositoryTests(PostgresEstoqueFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Constrói uma movimentação com data controlada (via Reconstituir), para
        /// exercitar os filtros de origem e de intervalo de datas do extrato.
        /// </summary>
        private static MovimentacaoDeEstoque Movimentacao(
            long id,
            long idProduto,
            TipoDeMovimentacao tipo,
            decimal quantidade,
            TipoOrigemMovimentacao origemTipo,
            long? origemIdReferencia,
            DateTime dataUtc)
        {
            var origem = OrigemDaMovimentacao.TentarCriar(origemTipo, origemIdReferencia).Instancia;

            return MovimentacaoDeEstoque.Reconstituir(
                Id.TentarCriar(idProduto).Instancia,
                tipo,
                TiposDeMovimentacao.Sentido(tipo),
                quantidade,
                saldoResultante: quantidade,
                origem,
                dataMovimentacaoUtc: DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc),
                id: id,
                dataCriacaoUtc: DateTime.UtcNow,
                dataAtualizacaoUtc: DateTime.UtcNow);
        }

        private async Task SalvarAsync(params MovimentacaoDeEstoque[] movimentacoes)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new MovimentacaoDeEstoqueRepository(contexto);

            foreach (var mov in movimentacoes)
                (await repositorio.AdicionarAsync(mov)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task Adicionar_DevePersistirComOrigemEmJsonbRecuperavel()
        {
            await SalvarAsync(Movimentacao(
                202607210600, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 30m,
                TipoOrigemMovimentacao.Compra, origemIdReferencia: 777,
                new DateTime(2026, 7, 1)));

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new MovimentacaoDeEstoqueRepository(contexto);

            var itens = (await repositorio.ListarPaginadoAsync(1, 10)).Instancia.Itens;

            itens.Should().ContainSingle();
            var mov = itens.Single();
            mov.Tipo.Should().Be(TipoDeMovimentacao.EntradaPorCompra);
            mov.Sentido.Should().Be(SentidoDaMovimentacao.Entrada);
            mov.Quantidade.Should().Be(30m);
            // A origem (VO composto) volta íntegra do jsonb.
            mov.Origem.Tipo.Should().Be(TipoOrigemMovimentacao.Compra);
            mov.Origem.IdReferencia.Should().Be(777);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorProdutoTipoEOrigem()
        {
            await SalvarAsync(
                Movimentacao(202607210610, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 10m,
                    TipoOrigemMovimentacao.Compra, 1, new DateTime(2026, 7, 1)),
                Movimentacao(202607210611, IdProdutoA, TipoDeMovimentacao.SaidaPorVenda, 4m,
                    TipoOrigemMovimentacao.Venda, 2, new DateTime(2026, 7, 2)),
                Movimentacao(202607210612, IdProdutoB, TipoDeMovimentacao.EntradaPorCompra, 7m,
                    TipoOrigemMovimentacao.Compra, 3, new DateTime(2026, 7, 3)));

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new MovimentacaoDeEstoqueRepository(contexto);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarMovimentacoesDeEstoqueFiltros(IdProduto: IdProdutoA))).Instancia
                .TotalRegistros.Should().Be(2);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarMovimentacoesDeEstoqueFiltros(Tipo: TipoDeMovimentacao.SaidaPorVenda))).Instancia
                .TotalRegistros.Should().Be(1);

            // Filtro por tipo de origem — resolvido DENTRO do jsonb pelo Postgres.
            var porOrigem = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarMovimentacoesDeEstoqueFiltros(OrigemTipo: TipoOrigemMovimentacao.Compra))).Instancia;
            porOrigem.TotalRegistros.Should().Be(2);
            porOrigem.Itens.Should().OnlyContain(m => m.Origem.Tipo == TipoOrigemMovimentacao.Compra);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorIntervaloDeDatas()
        {
            await SalvarAsync(
                Movimentacao(202607210620, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 1, new DateTime(2026, 6, 15)),
                Movimentacao(202607210621, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 2, new DateTime(2026, 7, 10)),
                Movimentacao(202607210622, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 3, new DateTime(2026, 8, 5)));

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new MovimentacaoDeEstoqueRepository(contexto);

            var julho = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarMovimentacoesDeEstoqueFiltros(
                    DataInicio: new DateTime(2026, 7, 1),
                    DataFim: new DateTime(2026, 7, 31)))).Instancia;

            julho.TotalRegistros.Should().Be(1);
            julho.Itens.Single().DataMovimentacaoUtc.Month.Should().Be(7);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveOrdenarDoMaisRecenteParaOMaisAntigo()
        {
            await SalvarAsync(
                Movimentacao(202607210630, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 1, new DateTime(2026, 7, 1)),
                Movimentacao(202607210631, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 2, new DateTime(2026, 7, 20)),
                Movimentacao(202607210632, IdProdutoA, TipoDeMovimentacao.EntradaPorCompra, 1m,
                    TipoOrigemMovimentacao.Compra, 3, new DateTime(2026, 7, 10)));

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new MovimentacaoDeEstoqueRepository(contexto);

            var itens = (await repositorio.ListarPaginadoAsync(1, 10)).Instancia.Itens.ToList();

            itens.Select(m => m.DataMovimentacaoUtc.Day).Should().ContainInOrder(20, 10, 1);
        }
    }
}
