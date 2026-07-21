using Microsoft.EntityFrameworkCore;

namespace simple_erp.Infraestrutura.Persistencia.Contexto
{
    public sealed class SimpleErpDbContext : DbContext
    {
        public SimpleErpDbContext(DbContextOptions<SimpleErpDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimpleErpDbContext).Assembly);
        }
    }
}
