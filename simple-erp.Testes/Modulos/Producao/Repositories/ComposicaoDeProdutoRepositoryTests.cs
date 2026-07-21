using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.UseCases;
using simple_erp.Infraestrutura.Repositorios.Producao;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class ComposicaoDeProdutoRepositoryTests
        : IClassFixture<PostgresProducaoFixture>, IAsyncLifetime
    {
        private const long IdProdutoFabricado = 202607210050;

        private readonly PostgresProducaoFixture _fixture;

        public ComposicaoDeProdutoRepositoryTests(PostgresProducaoFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        private async Task SalvarAsync(params ComposicaoDeProduto[] composicoes)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ComposicaoDeProdutoRepository(contexto);

            foreach (var composicao in composicoes)
                (await repositorio.AdicionarAsync(composicao)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirOsItensDaReceitaEmJsonb()
        {
            var composicao = ComposicaoDeProdutoBuilder.Novo()
                .ComId(202607211000)
                .ComIdProdutoFabricado(IdProdutoFabricado)
                .ComVersao(1)
                .SemItens()
                .ComItem(202607210010, 2m)
                .ComItem(202607210011, 3.5m)
                .Criar();

            await SalvarAsync(composicao);

            await using var contexto = _fixture.CriarContexto();
            var recuperada = (await new ComposicaoDeProdutoRepository(contexto)
                .ObterPorIdAsync(IdDe(202607211000))).Instancia;

            recuperada.Should().NotBeNull();
            recuperada!.IdProdutoFabricado.Valor.Should().Be(IdProdutoFabricado);
            recuperada.Versao.Should().Be(1);
            recuperada.Ativa.Should().BeFalse();
            recuperada.Itens.Should().HaveCount(2);
            recuperada.Itens.Single(i => i.IdInsumo == 202607210011)
                .QuantidadePorUnidade.Should().Be(3.5m);
        }

        [Fact]
        public async Task ExisteEObterAtivaPorProduto_DeveEncontrarSomenteAVersaoAtiva()
        {
            await SalvarAsync(
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211010).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar(),
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211011).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(2).SemItens().ComItem(202607210010, 1m).Ativa().Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ComposicaoDeProdutoRepository(contexto);

            (await repositorio.ExisteAtivaPorProdutoAsync(IdDe(IdProdutoFabricado))).Instancia
                .Should().BeTrue();

            var ativa = (await repositorio.ObterAtivaPorProdutoAsync(IdDe(IdProdutoFabricado))).Instancia;
            ativa.Should().NotBeNull();
            ativa!.Versao.Should().Be(2);
        }

        [Fact]
        public async Task ObterProximaVersaoPorProduto_DeveComecarEm1EIncrementar()
        {
            var repositorioVazio = new ComposicaoDeProdutoRepository(_fixture.CriarContexto());
            (await repositorioVazio.ObterProximaVersaoPorProdutoAsync(IdDe(IdProdutoFabricado))).Instancia
                .Should().Be(1, "sem versões, a primeira é 1");

            await SalvarAsync(
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211020).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar(),
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211021).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(2).SemItens().ComItem(202607210010, 1m).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ComposicaoDeProdutoRepository(contexto);

            (await repositorio.ObterProximaVersaoPorProdutoAsync(IdDe(IdProdutoFabricado))).Instancia
                .Should().Be(3, "a maior versão é 2");
        }

        [Fact]
        public async Task Adicionar_MesmaVersaoParaOMesmoProduto_DeveViolarOIndiceUnico()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ComposicaoDeProdutoRepository(contexto);

            await repositorio.AdicionarAsync(
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211030).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar());
            await repositorio.AdicionarAsync(
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211031).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar());

            var acao = async () => await contexto.SaveChangesAsync();

            await acao.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ListarPorProdutoPaginadoAsync_DeveFiltrarPorProdutoEApenasAtivas()
        {
            await SalvarAsync(
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211040).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar(),
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211041).ComIdProdutoFabricado(IdProdutoFabricado)
                    .ComVersao(2).SemItens().ComItem(202607210010, 1m).Ativa().Criar(),
                ComposicaoDeProdutoBuilder.Novo().ComId(202607211042).ComIdProdutoFabricado(202607219999)
                    .ComVersao(1).SemItens().ComItem(202607210010, 1m).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ComposicaoDeProdutoRepository(contexto);

            var todasDoProduto = (await repositorio.ListarPorProdutoPaginadoAsync(
                1, 10, new ListarVersoesDeComposicaoFiltros(IdProdutoFabricado))).Instancia;
            todasDoProduto.TotalRegistros.Should().Be(2);
            // Ordenação decrescente por versão: a mais recente primeiro.
            todasDoProduto.Itens.Select(c => c.Versao).Should().ContainInOrder(2, 1);

            var apenasAtivas = (await repositorio.ListarPorProdutoPaginadoAsync(
                1, 10, new ListarVersoesDeComposicaoFiltros(IdProdutoFabricado, ApenasAtivas: true))).Instancia;
            apenasAtivas.TotalRegistros.Should().Be(1);
            apenasAtivas.Itens.Single().Versao.Should().Be(2);
        }
    }
}
