using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.UseCases;
using simple_erp.Infraestrutura.Repositorios.Vendas;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Vendas.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class PedidoDeVendaRepositoryTests
        : IClassFixture<PostgresVendasFixture>, IAsyncLifetime
    {
        private const long IdCliente = 202607210001;

        private readonly PostgresVendasFixture _fixture;

        public PedidoDeVendaRepositoryTests(PostgresVendasFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        private async Task SalvarAsync(params PedidoDeVenda[] pedidos)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeVendaRepository(contexto);

            foreach (var pedido in pedidos)
                (await repositorio.AdicionarAsync(pedido)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirItensDescontoEValorTotal()
        {
            var pedido = PedidoDeVendaBuilder.Novo()
                .ComId(202607211100)
                .ComNumero(1)
                .ComIdCliente(IdCliente)
                .SemItens() // limpa o item padrão do builder para partir do zero
                .ComItem(202607210010, quantidade: 4m, precoUnitario: 10m, desconto: 0m)
                .ComItem(202607210011, quantidade: 2m, precoUnitario: 25m, desconto: 5m)
                .ComDescontoDoPedido(10m)
                .EmEdicao()
                .Criar();

            await SalvarAsync(pedido);

            await using var contexto = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeVendaRepository(contexto)
                .ObterPorIdAsync(IdDe(202607211100))).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.Numero.Should().Be(1);
            recuperado.IdCliente.Valor.Should().Be(IdCliente);
            recuperado.Status.Should().Be(StatusPedidoDeVenda.EmEdicao);
            recuperado.Itens.Should().HaveCount(2);
            recuperado.DescontoDoPedido.Should().Be(10m);

            // Subtotal: (4*10 - 0) + (2*25 - 5) = 40 + 45 = 85; menos desconto do pedido 10 = 75.
            recuperado.ValorTotal.Valor.Should().Be(75m);
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeVendaRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(IdDe(999999999999));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
            (await repositorio.ExistePorIdAsync(IdDe(999999999999))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task ObterProximoNumeroAsync_DeveComecarEm1EIncrementar()
        {
            var repositorioVazio = new PedidoDeVendaRepository(_fixture.CriarContexto());
            (await repositorioVazio.ObterProximoNumeroAsync()).Instancia
                .Should().Be(1, "sem pedidos, o primeiro número é 1");

            await SalvarAsync(
                PedidoDeVendaBuilder.Novo().ComId(202607211110).ComNumero(1).ComIdCliente(IdCliente).Criar(),
                PedidoDeVendaBuilder.Novo().ComId(202607211111).ComNumero(2).ComIdCliente(IdCliente).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeVendaRepository(contexto);

            (await repositorio.ObterProximoNumeroAsync()).Instancia
                .Should().Be(3, "o maior número é 2");
        }

        [Fact]
        public async Task Adicionar_NumeroDuplicado_DeveViolarOIndiceUnico()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeVendaRepository(contexto);

            await repositorio.AdicionarAsync(
                PedidoDeVendaBuilder.Novo().ComId(202607211120).ComNumero(7).ComIdCliente(IdCliente).Criar());
            await repositorio.AdicionarAsync(
                PedidoDeVendaBuilder.Novo().ComId(202607211121).ComNumero(7).ComIdCliente(IdCliente).Criar());

            var acao = async () => await contexto.SaveChangesAsync();

            await acao.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task EditarItens_ViaEntidadeRastreada_DevePersistirAsAlteracoesDaColecao()
        {
            await SalvarAsync(PedidoDeVendaBuilder.Novo()
                .ComId(202607211130).ComNumero(10).ComIdCliente(IdCliente)
                .SemItens() // limpa o item padrão do builder para partir do zero
                .ComItem(202607210010, 5m, 2m)
                .EmEdicao().Criar());

            // Fluxo real: obter (rastreado) → adicionar/remover item → Atualizar → SaveChanges.
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new PedidoDeVendaRepository(contexto);
                var pedido = (await repositorio.ObterPorIdAsync(IdDe(202607211130))).Instancia!;

                var novoItem = ItemDePedidoDeVenda.TentarCriar(
                    IdDe(202607210011),
                    Quantidade.TentarCriar(3m).Instancia,
                    Dinheiro.TentarCriar(4m).Instancia,
                    Dinheiro.TentarCriar(0m).Instancia).Instancia;

                pedido.AdicionarItem(novoItem).EhSucesso.Should().BeTrue();
                pedido.RemoverItem(202607210010).EhSucesso.Should().BeTrue();

                (await repositorio.AtualizarAsync(pedido)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeVendaRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607211130))).Instancia!;

            recuperado.Itens.Should().ContainSingle();
            recuperado.Itens.Single().IdProduto.Should().Be(202607210011);
            recuperado.ValorTotal.Valor.Should().Be(12m, "3 * 4.00");
        }

        [Fact]
        public async Task Cancelar_ViaEntidadeRastreada_DevePersistirStatusEMotivo()
        {
            await SalvarAsync(PedidoDeVendaBuilder.Novo()
                .ComId(202607211140).ComNumero(11).ComIdCliente(IdCliente)
                .EmEdicao().Criar());

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new PedidoDeVendaRepository(contexto);
                var pedido = (await repositorio.ObterPorIdAsync(IdDe(202607211140))).Instancia!;

                var motivo = MotivoCancelamento.TentarCriar("Cliente desistiu da compra").Instancia;
                pedido.Cancelar(motivo).EhSucesso.Should().BeTrue();

                await repositorio.AtualizarAsync(pedido);
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new PedidoDeVendaRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607211140))).Instancia!;

            recuperado.Status.Should().Be(StatusPedidoDeVenda.Cancelado);
            recuperado.MotivoCancelamento.Should().Be("Cliente desistiu da compra");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorClienteEStatus()
        {
            await SalvarAsync(
                PedidoDeVendaBuilder.Novo().ComId(202607211150).ComNumero(20).ComIdCliente(IdCliente)
                    .EmEdicao().Criar(),
                PedidoDeVendaBuilder.Novo().ComId(202607211151).ComNumero(21).ComIdCliente(IdCliente)
                    .Aprovado().Criar(),
                PedidoDeVendaBuilder.Novo().ComId(202607211152).ComNumero(22).ComIdCliente(202607219999)
                    .EmEdicao().Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new PedidoDeVendaRepository(contexto);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarPedidosDeVendaFiltros(IdCliente: IdCliente))).Instancia
                .TotalRegistros.Should().Be(2);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarPedidosDeVendaFiltros(Status: StatusPedidoDeVenda.Aprovado))).Instancia
                .TotalRegistros.Should().Be(1);
        }
    }
}
