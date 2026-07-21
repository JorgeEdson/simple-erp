using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Infraestrutura.Repositorios.Suprimentos;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Suprimentos.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class PedidoDeCompraRepositoryTests
        : IClassFixture<PostgresSuprimentosFixture>, IAsyncLifetime
    {
        private const long IdFornecedor = 202607210002;

        private readonly PostgresSuprimentosFixture _fixture;

        public PedidoDeCompraRepositoryTests(PostgresSuprimentosFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        private async Task SalvarAsync(params PedidoDeCompra[] pedidos)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeCompraRepository(contexto);

            foreach (var pedido in pedidos)
                (await repositorio.AdicionarAsync(pedido)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirOsItensEmJsonb()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .ComId(202607210800)
                .ComIdFornecedor(IdFornecedor)
                .SemItens() // limpa o item padrão do builder para partir do zero
                .ComItem(202607210010, quantidade: 5m, custoUnitario: 2.50m)
                .ComItem(202607210011, quantidade: 3m, custoUnitario: 4.00m)
                .EmEdicao()
                .Criar();

            await SalvarAsync(pedido);

            await using var contexto = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeCompraRepository(contexto)
                .ObterPorIdAsync(IdDe(202607210800))).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.IdFornecedor.Valor.Should().Be(IdFornecedor);
            recuperado.Status.Should().Be(StatusPedidoDeCompra.EmEdicao);
            recuperado.Itens.Should().HaveCount(2);

            var primeiro = recuperado.Itens.Single(i => i.IdProduto == 202607210010);
            primeiro.Quantidade.Should().Be(5m);
            primeiro.CustoUnitario.Should().Be(2.50m);
            primeiro.Subtotal.Should().Be(12.50m);

            // O total é derivado dos itens recuperados do jsonb.
            recuperado.ValorTotal.Valor.Should().Be(24.50m, "5*2.50 + 3*4.00");
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeCompraRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(IdDe(999999999999));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
            (await repositorio.ExistePorIdAsync(IdDe(999999999999))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task EditarItens_ViaEntidadeRastreada_DevePersistirAsAlteracoesDaColecao()
        {
            await SalvarAsync(PedidoDeCompraBuilder.Novo()
                .ComId(202607210810).ComIdFornecedor(IdFornecedor)
                .SemItens() // limpa o item padrão do builder para partir do zero
                .ComItem(202607210010, 5m, 2m)
                .EmEdicao().Criar());

            // Fluxo real: obter (rastreado) → adicionar/remover item → Atualizar → SaveChanges.
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new PedidoDeCompraRepository(contexto);
                var pedido = (await repositorio.ObterPorIdAsync(IdDe(202607210810))).Instancia!;

                var novoItem = ItemDePedidoDeCompra.TentarCriar(
                    IdDe(202607210011),
                    Quantidade.TentarCriar(10m).Instancia,
                    Dinheiro.TentarCriar(1.5m).Instancia).Instancia;

                pedido.AdicionarItem(novoItem).EhSucesso.Should().BeTrue();
                pedido.RemoverItem(202607210010).EhSucesso.Should().BeTrue();

                (await repositorio.AtualizarAsync(pedido)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeCompraRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607210810))).Instancia!;

            recuperado.Itens.Should().ContainSingle();
            recuperado.Itens.Single().IdProduto.Should().Be(202607210011);
            recuperado.ValorTotal.Valor.Should().Be(15m, "10 * 1.50");
        }

        [Fact]
        public async Task AprovarEfetivar_ViaEntidadeRastreada_DevePersistirATransicaoDeStatus()
        {
            await SalvarAsync(PedidoDeCompraBuilder.Novo()
                .ComId(202607210820).ComIdFornecedor(IdFornecedor)
                .ComItem(202607210010, 2m, 5m)
                .EmEdicao().Criar());

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new PedidoDeCompraRepository(contexto);
                var pedido = (await repositorio.ObterPorIdAsync(IdDe(202607210820))).Instancia!;

                pedido.Aprovar().EhSucesso.Should().BeTrue();
                pedido.Efetivar().EhSucesso.Should().BeTrue();

                await repositorio.AtualizarAsync(pedido);
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeCompraRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607210820))).Instancia!;

            recuperado.Status.Should().Be(StatusPedidoDeCompra.Concluida);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorFornecedorEStatus()
        {
            await SalvarAsync(
                PedidoDeCompraBuilder.Novo().ComId(202607210830).ComIdFornecedor(IdFornecedor)
                    .ComItem(202607210010, 1m, 1m).EmEdicao().Criar(),
                PedidoDeCompraBuilder.Novo().ComId(202607210831).ComIdFornecedor(IdFornecedor)
                    .ComItem(202607210010, 1m, 1m).Aprovado().Criar(),
                PedidoDeCompraBuilder.Novo().ComId(202607210832).ComIdFornecedor(202607219999)
                    .ComItem(202607210010, 1m, 1m).EmEdicao().Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeCompraRepository(contexto);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarPedidosDeCompraFiltros(IdFornecedor: IdFornecedor))).Instancia
                .TotalRegistros.Should().Be(2);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarPedidosDeCompraFiltros(Status: StatusPedidoDeCompra.Aprovada))).Instancia
                .TotalRegistros.Should().Be(1);
        }
    }
}
