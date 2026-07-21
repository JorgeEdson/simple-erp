using Microsoft.EntityFrameworkCore;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using Testcontainers.PostgreSql;

namespace simple_erp.Testes.Modulos.Financeiro.Repositories
{
    /// <summary>
    /// Postgres real e descartável (Testcontainers) para os testes de repositório do
    /// Financeiro. Cria o schema do modelo EF e trunca a tabela entre testes. Requer
    /// Docker; as classes que a usam levam [Trait("Categoria","Integracao")].
    /// </summary>
    public sealed class PostgresFinanceiroFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();

        private DbContextOptions<SimpleErpDbContext> _options = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            _options = new DbContextOptionsBuilder<SimpleErpDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .UseSnakeCaseNamingConvention()
                .Options;

            await using var contexto = CriarContexto();
            await contexto.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public SimpleErpDbContext CriarContexto() => new(_options);

        public async Task LimparAsync()
        {
            await using var contexto = CriarContexto();
            await contexto.Database.ExecuteSqlRawAsync("TRUNCATE TABLE financeiro.titulos");
        }
    }
}
