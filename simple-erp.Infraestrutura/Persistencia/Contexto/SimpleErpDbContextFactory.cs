using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace simple_erp.Infraestrutura.Persistencia.Contexto
{
    /// <summary>
    /// Factory usada apenas pelo tooling do EF (dotnet ef migrations/database) em
    /// tempo de design, já que o projeto ainda não tem um host executável. A
    /// connection string pode ser sobrescrita pela variável de ambiente
    /// SIMPLE_ERP_CONNECTION_STRING; o padrão aponta para o Postgres do
    /// docker-compose na raiz do repositório.
    /// </summary>
    public sealed class SimpleErpDbContextFactory : IDesignTimeDbContextFactory<SimpleErpDbContext>
    {
        private const string ConnectionStringPadrao =
            "Host=localhost;Port=5432;Database=simple_erp;Username=simple_erp;Password=simple_erp";

        public SimpleErpDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("SIMPLE_ERP_CONNECTION_STRING")
                ?? ConnectionStringPadrao;

            var options = new DbContextOptionsBuilder<SimpleErpDbContext>()
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new SimpleErpDbContext(options);
        }
    }
}
