using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Infraestrutura.Repositorios.Estoque;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Estoque.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class SaldoDeEstoqueRepositoryTests
        : IClassFixture<PostgresEstoqueFixture>, IAsyncLifetime
    {
        private const long IdProduto = 202607210001;

        private readonly PostgresEstoqueFixture _fixture;

        public SaldoDeEstoqueRepositoryTests(PostgresEstoqueFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        [Fact]
        public async Task AdicionarEObterPorProduto_DevePersistirERecuperarOSaldo()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo()
                .ComId(202607210500)
                .ComIdProduto(IdProduto)
                .ComSaldoInicial(25m)
                .Criar();

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new SaldoDeEstoqueRepository(contexto);
                (await repositorio.AdicionarAsync(saldo)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new SaldoDeEstoqueRepository(contextoLeitura)
                .ObterPorProdutoAsync(IdDe(IdProduto))).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.IdProduto.Valor.Should().Be(IdProduto);
            recuperado.QuantidadeAtual.Should().Be(25m);
        }

        [Fact]
        public async Task ObterPorProdutoAsync_DeveRetornarSucessoComNulo_QuandoNaoExiste()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new SaldoDeEstoqueRepository(contexto);

            var resultado = await repositorio.ObterPorProdutoAsync(IdDe(999999999999));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();

            (await repositorio.ExistePorProdutoAsync(IdDe(999999999999))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task ObterPorProdutoAsync_DeveRetornarEntidadeRastreada_PermitindoMovimentarEAtualizar()
        {
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new SaldoDeEstoqueRepository(contexto);
                await repositorio.AdicionarAsync(
                    SaldoDeEstoqueBuilder.Novo().ComId(202607210510).ComIdProduto(IdProduto)
                        .ComSaldoInicial(10m).Criar());
                await contexto.SaveChangesAsync();
            }

            // Fluxo real de escrita: obter (rastreado) → Movimentar → Atualizar → SaveChanges.
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new SaldoDeEstoqueRepository(contexto);
                var saldo = (await repositorio.ObterPorProdutoAsync(IdDe(IdProduto))).Instancia!;

                var origem = OrigemDaMovimentacao.TentarCriar(TipoOrigemMovimentacao.Venda, 555).Instancia;
                var quantidade = Quantidade.TentarCriar(4m).Instancia;
                var mov = saldo.Movimentar(TipoDeMovimentacao.SaidaPorVenda, quantidade, origem);
                mov.EhSucesso.Should().BeTrue();

                (await repositorio.AtualizarAsync(saldo)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new SaldoDeEstoqueRepository(contextoLeitura)
                .ObterPorProdutoAsync(IdDe(IdProduto))).Instancia!;

            recuperado.QuantidadeAtual.Should().Be(6m, "10 inicial - 4 de saída");
        }

        [Fact]
        public async Task Adicionar_DoisSaldosParaOMesmoProduto_DeveViolarOIndiceUnico()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new SaldoDeEstoqueRepository(contexto);

            await repositorio.AdicionarAsync(
                SaldoDeEstoqueBuilder.Novo().ComId(202607210520).ComIdProduto(IdProduto).Criar());
            await repositorio.AdicionarAsync(
                SaldoDeEstoqueBuilder.Novo().ComId(202607210521).ComIdProduto(IdProduto).Criar());

            // A invariante "um saldo por produto" é garantida no banco pelo índice único.
            var acao = async () => await contexto.SaveChangesAsync();

            await acao.Should().ThrowAsync<Exception>();
        }
    }
}
