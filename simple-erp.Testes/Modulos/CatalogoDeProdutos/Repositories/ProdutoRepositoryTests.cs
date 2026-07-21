using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases;
using simple_erp.Infraestrutura.Repositorios.CatalogoDeProdutos;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class ProdutoRepositoryTests
        : IClassFixture<PostgresCatalogoFixture>, IAsyncLifetime
    {
        private readonly PostgresCatalogoFixture _fixture;

        public ProdutoRepositoryTests(PostgresCatalogoFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SalvarAsync(params Produto[] produtos)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            foreach (var produto in produtos)
                (await repositorio.AdicionarAsync(produto)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        private static CodigoProduto Codigo(string valor) => CodigoProduto.TentarCriar(valor).Instancia;

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirERecuperarTodosOsValueObjects()
        {
            var produto = ProdutoBuilder.Novo()
                .ComId(202607210001)
                .ComCodigo("PROD-001")
                .ComDescricao("Parafuso sextavado M8")
                .ComUnidadeDeMedida("PC")
                .ComoMateriaPrima()
                .Criar();

            await SalvarAsync(produto);

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            var recuperado = (await repositorio.ObterPorIdAsync(
                Id.TentarCriar(202607210001).Instancia)).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.Id.Valor.Should().Be(202607210001);
            recuperado.Codigo.Valor.Should().Be("PROD-001");
            recuperado.Descricao.Valor.Should().Be("Parafuso sextavado M8");
            recuperado.UnidadeDeMedida.Valor.Should().Be("PC");
            recuperado.Classificacao.Should().Be(ClassificacaoProduto.MateriaPrima);
            recuperado.EhMateriaPrima.Should().BeTrue();
            recuperado.Ativo.Should().BeTrue();
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(Id.TentarCriar(999999999999).Instancia);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
        }

        [Fact]
        public async Task ObterPorCodigoAsync_DeveNormalizarParaMaiusculas()
        {
            await SalvarAsync(ProdutoBuilder.Novo().ComId(202607210010).ComCodigo("ABC-123").Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            // O VO CodigoProduto normaliza para maiúsculas ao ser criado,
            // então buscar por "abc-123" encontra o registro gravado como "ABC-123".
            var resultado = await repositorio.ObterPorCodigoAsync(Codigo("abc-123"));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().NotBeNull();
            resultado.Instancia!.Id.Valor.Should().Be(202607210010);

            (await repositorio.ExistePorCodigoAsync(Codigo("ABC-123"))).Instancia.Should().BeTrue();
            (await repositorio.ExistePorCodigoAsync(Codigo("NAO-EXISTE"))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task ExisteOutroPorCodigoAsync_DeveIgnorarOProprioProduto()
        {
            await SalvarAsync(
                ProdutoBuilder.Novo().ComId(202607210020).ComCodigo("COD-A").Criar(),
                ProdutoBuilder.Novo().ComId(202607210021).ComCodigo("COD-B").Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);
            var codigo = Codigo("COD-A");

            (await repositorio.ExisteOutroPorCodigoAsync(
                Id.TentarCriar(202607210020).Instancia, codigo)).Instancia.Should().BeFalse();
            (await repositorio.ExisteOutroPorCodigoAsync(
                Id.TentarCriar(202607210021).Instancia, codigo)).Instancia.Should().BeTrue();
        }

        [Fact]
        public async Task AtualizarAsync_DevePersistirAlteracoesDaEntidadeRastreada()
        {
            await SalvarAsync(ProdutoBuilder.Novo().ComId(202607210030).ComCodigo("EDIT-1").Criar());

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new ProdutoRepository(contexto);
                var produto = (await repositorio.ObterPorIdAsync(
                    Id.TentarCriar(202607210030).Instancia)).Instancia!;

                produto.AlterarDescricao(DescricaoProduto.TentarCriar("Descrição Editada").Instancia);
                produto.ClassificarComoFabricado();
                (await repositorio.AtualizarAsync(produto)).EhSucesso.Should().BeTrue();

                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new ProdutoRepository(contextoLeitura).ObterPorIdAsync(
                Id.TentarCriar(202607210030).Instancia)).Instancia!;

            recuperado.Descricao.Valor.Should().Be("Descrição Editada");
            recuperado.Classificacao.Should().Be(ClassificacaoProduto.Fabricado);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarNoBanco_PorTodosOsCampos()
        {
            await SalvarAsync(
                ProdutoBuilder.Novo().ComId(202607210040).ComCodigo("MP-001").ComDescricao("Aço laminado")
                    .ComUnidadeDeMedida("KG").ComoMateriaPrima().Criar(),
                ProdutoBuilder.Novo().ComId(202607210041).ComCodigo("MP-002").ComDescricao("Aço inox")
                    .ComUnidadeDeMedida("KG").ComoMateriaPrima().Inativo().Criar(),
                ProdutoBuilder.Novo().ComId(202607210042).ComCodigo("FAB-001").ComDescricao("Cadeira montada")
                    .ComUnidadeDeMedida("UN").ComoFabricado().Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            // Descrição (ILIKE)
            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarProdutosFiltros(Descricao: "aço"))).Instancia
                .TotalRegistros.Should().Be(2);

            // Unidade de medida (igualdade exata)
            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarProdutosFiltros(UnidadeDeMedida: "kg"))).Instancia
                .TotalRegistros.Should().Be(2);

            // Classificação
            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarProdutosFiltros(Classificacao: ClassificacaoProduto.Fabricado))).Instancia
                .TotalRegistros.Should().Be(1);

            // Ativo
            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarProdutosFiltros(Ativo: false))).Instancia
                .TotalRegistros.Should().Be(1);

            // Combinados: matéria-prima ativa
            var mpAtivas = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarProdutosFiltros(
                    Classificacao: ClassificacaoProduto.MateriaPrima, Ativo: true))).Instancia;
            mpAtivas.TotalRegistros.Should().Be(1);
            mpAtivas.Itens.Should().ContainSingle(p => p.Codigo.Valor == "MP-001");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DevePaginarOrdenandoPorCodigo()
        {
            await SalvarAsync(
                ProdutoBuilder.Novo().ComId(202607210050).ComCodigo("C-003").Criar(),
                ProdutoBuilder.Novo().ComId(202607210051).ComCodigo("C-001").Criar(),
                ProdutoBuilder.Novo().ComId(202607210052).ComCodigo("C-002").Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ProdutoRepository(contexto);

            var pagina1 = (await repositorio.ListarPaginadoAsync(1, 2)).Instancia;
            pagina1.TotalRegistros.Should().Be(3);
            pagina1.TotalPaginas.Should().Be(2);
            pagina1.Itens.Select(p => p.Codigo.Valor).Should().ContainInOrder("C-001", "C-002");

            var pagina2 = (await repositorio.ListarPaginadoAsync(2, 2)).Instancia;
            pagina2.Itens.Should().ContainSingle(p => p.Codigo.Valor == "C-003");
        }
    }
}
