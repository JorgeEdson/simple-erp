using Microsoft.EntityFrameworkCore;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using Testcontainers.PostgreSql;

namespace simple_erp.Testes.Modulos.CatalogoDeProdutos.Repositories
{   
    public sealed class PostgresCatalogoFixture : IAsyncLifetime
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
            await contexto.Database.ExecuteSqlRawAsync("TRUNCATE TABLE catalogo.produtos");
        }
    }
}
