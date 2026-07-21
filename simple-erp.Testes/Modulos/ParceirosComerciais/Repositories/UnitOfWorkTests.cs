using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using simple_erp.Infraestrutura.Repositorios.ParceirosComerciais;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class UnitOfWorkTests : IClassFixture<PostgresParceirosFixture>, IAsyncLifetime
    {
        private const string CpfA = "12345678909";

        private readonly PostgresParceirosFixture _fixture;

        public UnitOfWorkTests(PostgresParceirosFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private static UnitOfWork CriarUnitOfWork(SimpleErpDbContext contexto) =>
            new(contexto, new ClienteRepository(contexto), new FornecedorRepository(contexto));

        [Fact]
        public async Task SaveChangesAsync_DevePersistirOTrabalhoDosRepositorios()
        {
            await using (var contexto = _fixture.CriarContexto())
            {
                var uow = CriarUnitOfWork(contexto);

                await uow.ClientesRepository.AdicionarAsync(
                    ClienteBuilder.Novo().ComId(202607210200).ComDocumento(CpfA).Criar());

                var resultado = await uow.SaveChangesAsync();

                resultado.EhSucesso.Should().BeTrue();
                resultado.Instancia.Should().Be(1, "um registro deve ter sido afetado");
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var existe = await CriarUnitOfWork(contextoLeitura).ClientesRepository
                .ExistePorIdAsync(Id.TentarCriar(202607210200).Instancia);

            existe.Instancia.Should().BeTrue();
        }

        [Fact]
        public async Task SaveChangesAsync_DeveRetornarFalha_QuandoDocumentoDuplicado()
        {
            await using var contexto = _fixture.CriarContexto();
            var uow = CriarUnitOfWork(contexto);

            await uow.ClientesRepository.AdicionarAsync(
                ClienteBuilder.Novo().ComId(202607210210).ComDocumento(CpfA).Criar());
            (await uow.SaveChangesAsync()).EhSucesso.Should().BeTrue();

            await uow.ClientesRepository.AdicionarAsync(
                ClienteBuilder.Novo().ComId(202607210211).ComDocumento(CpfA).Criar());

            var resultado = await uow.SaveChangesAsync();

            // O índice único do banco é a última linha de defesa da regra de negócio —
            // a violação vira Resultado.Falha, nunca exceção estourada no use case.
            resultado.EhFalha.Should().BeTrue();
            resultado.Erros!.First().Should().Contain("ux_clientes_documento");
        }

        [Fact]
        public async Task RollbackTransactionAsync_DeveDescartarOTrabalhoDaTransacao()
        {
            await using (var contexto = _fixture.CriarContexto())
            {
                var uow = CriarUnitOfWork(contexto);

                (await uow.BeginTransactionAsync()).EhSucesso.Should().BeTrue();

                await uow.ClientesRepository.AdicionarAsync(
                    ClienteBuilder.Novo().ComId(202607210220).ComDocumento(CpfA).Criar());
                await uow.SaveChangesAsync();

                (await uow.RollbackTransactionAsync()).EhSucesso.Should().BeTrue();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var existe = await CriarUnitOfWork(contextoLeitura).ClientesRepository
                .ExistePorIdAsync(Id.TentarCriar(202607210220).Instancia);

            existe.Instancia.Should().BeFalse("o rollback deve descartar o insert");
        }

        [Fact]
        public async Task CommitTransactionAsync_DeveRetornarFalha_SemTransacaoAtiva()
        {
            await using var contexto = _fixture.CriarContexto();
            var uow = CriarUnitOfWork(contexto);

            var resultado = await uow.CommitTransactionAsync();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SEM_TRANSACAO_ATIVA");
        }

        [Fact]
        public async Task RepositoriosNaoImplementados_DevemLancarComMensagemDoPlanoIncremental()
        {
            await using var contexto = _fixture.CriarContexto();
            var uow = CriarUnitOfWork(contexto);

            var acao = () => uow.ProdutosRepository;

            acao.Should().Throw<NotImplementedException>()
                .WithMessage("*Catálogo de Produtos*");
        }
    }
}
