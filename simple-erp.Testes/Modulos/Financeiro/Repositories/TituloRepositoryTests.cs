using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Infraestrutura.Repositorios.Financeiro;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Financeiro.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class TituloRepositoryTests
        : IClassFixture<PostgresFinanceiroFixture>, IAsyncLifetime
    {
        private const long IdParceiro = 202607210002;

        private readonly PostgresFinanceiroFixture _fixture;

        public TituloRepositoryTests(PostgresFinanceiroFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static Id IdDe(long valor) => Id.TentarCriar(valor).Instancia;

        private async Task SalvarAsync(params Titulo[] titulos)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new TituloRepository(contexto);

            foreach (var titulo in titulos)
                (await repositorio.AdicionarAsync(titulo)).EhSucesso.Should().BeTrue();

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirERecuperarOTituloComOrigem()
        {
            var titulo = TituloBuilder.Novo()
                .ComId(202607210700)
                .ComoAPagar()
                .ComIdParceiro(IdParceiro)
                .ComValorOriginal(500m)
                .Criar();

            await SalvarAsync(titulo);

            await using var contexto = _fixture.CriarContexto();
            var recuperado = (await new TituloRepository(contexto)
                .ObterPorIdAsync(IdDe(202607210700))).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.Tipo.Should().Be(TipoDeTitulo.APagar);
            recuperado.IdParceiro.Valor.Should().Be(IdParceiro);
            recuperado.ValorOriginal.Should().Be(500m);
            recuperado.Status.Should().Be(StatusTitulo.EmAberto);
            // A origem (VO composto) volta íntegra do jsonb.
            recuperado.Origem.Tipo.Should().Be(TipoOrigemTitulo.Compra);
            recuperado.SaldoDevedor.Should().Be(500m);
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new TituloRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(IdDe(999999999999));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
            (await repositorio.ExistePorIdAsync(IdDe(999999999999))).Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task Titulo_DevePersistirORecuperarOHistoricoDeBaixas()
        {
            // Título com uma baixa parcial aplicada no builder.
            var titulo = TituloBuilder.Novo()
                .ComId(202607210710)
                .ComoAReceber()
                .ComIdParceiro(IdParceiro)
                .ComValorOriginal(300m)
                .ComBaixaInicial(120m)
                .Criar();

            await SalvarAsync(titulo);

            await using var contexto = _fixture.CriarContexto();
            var recuperado = (await new TituloRepository(contexto)
                .ObterPorIdAsync(IdDe(202607210710))).Instancia!;

            // A coleção de VOs (histórico de baixas) volta do jsonb com valores íntegros.
            recuperado.Baixas.Should().HaveCount(1);
            recuperado.Baixas.Single().Montante.Should().Be(120m);
            recuperado.ValorBaixado.Should().Be(120m);
            recuperado.SaldoDevedor.Should().Be(180m);
            recuperado.Status.Should().Be(StatusTitulo.ParcialmenteBaixado);
        }

        [Fact]
        public async Task Baixar_ViaEntidadeRastreada_DeveAcrescentarAoHistoricoEPersistir()
        {
            await SalvarAsync(TituloBuilder.Novo()
                .ComId(202607210720).ComoAReceber().ComIdParceiro(IdParceiro)
                .ComValorOriginal(200m).Criar());

            // Fluxo real: obter (rastreado) → Baixar → Atualizar → SaveChanges.
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new TituloRepository(contexto);
                var titulo = (await repositorio.ObterPorIdAsync(IdDe(202607210720))).Instancia!;

                titulo.Baixar(Dinheiro.TentarCriar(80m).Instancia).EhSucesso.Should().BeTrue();
                titulo.Baixar(Dinheiro.TentarCriar(120m).Instancia).EhSucesso.Should().BeTrue();

                (await repositorio.AtualizarAsync(titulo)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var recuperado = (await new TituloRepository(contextoLeitura)
                .ObterPorIdAsync(IdDe(202607210720))).Instancia!;

            recuperado.Baixas.Should().HaveCount(2);
            recuperado.ValorBaixado.Should().Be(200m);
            recuperado.SaldoDevedor.Should().Be(0m);
            recuperado.Status.Should().Be(StatusTitulo.Liquidado, "as baixas quitaram o título");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorTipoStatusEParceiro()
        {
            await SalvarAsync(
                TituloBuilder.Novo().ComId(202607210730).ComoAPagar().ComIdParceiro(IdParceiro)
                    .ComValorOriginal(100m).Criar(),
                TituloBuilder.Novo().ComId(202607210731).ComoAReceber().ComIdParceiro(IdParceiro)
                    .ComValorOriginal(100m).ComBaixaInicial(40m).Criar(),
                TituloBuilder.Novo().ComId(202607210732).ComoAReceber().ComIdParceiro(202607219999)
                    .ComValorOriginal(100m).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new TituloRepository(contexto);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarTitulosFiltros(Tipo: TipoDeTitulo.AReceber))).Instancia
                .TotalRegistros.Should().Be(2);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarTitulosFiltros(Status: StatusTitulo.ParcialmenteBaixado))).Instancia
                .TotalRegistros.Should().Be(1);

            (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarTitulosFiltros(IdParceiro: IdParceiro))).Instancia
                .TotalRegistros.Should().Be(2);
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarPorIntervaloDeVencimento()
        {
            var hoje = DateTime.UtcNow.Date;

            await SalvarAsync(
                TituloBuilder.Novo().ComId(202607210740).ComoAPagar().ComIdParceiro(IdParceiro)
                    .ComValorOriginal(100m).ComVencimento(hoje.AddDays(10)).Criar(),
                TituloBuilder.Novo().ComId(202607210741).ComoAPagar().ComIdParceiro(IdParceiro)
                    .ComValorOriginal(100m).ComVencimento(hoje.AddDays(60)).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new TituloRepository(contexto);

            var proximos30 = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarTitulosFiltros(
                    VencimentoInicio: hoje,
                    VencimentoFim: hoje.AddDays(30)))).Instancia;

            proximos30.TotalRegistros.Should().Be(1);
        }
    }
}
