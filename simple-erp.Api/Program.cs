using simple_erp.Api.Configuracao;
using simple_erp.Infraestrutura.Extensoes;

var builder = WebApplication.CreateBuilder(args);

// Connection string do Postgres (docker-compose na raiz do repositório).
var connectionString =
    builder.Configuration.GetConnectionString("SimpleErp")
    ?? throw new InvalidOperationException(
        "Connection string 'SimpleErp' não configurada (veja appsettings.json).");

// Camadas: infraestrutura (DbContext, UnitOfWork, repositórios) + aplicação (use cases).
builder.Services.AdicionarInfraestrutura(connectionString);
builder.Services.AdicionarAplicacao();

builder.Services.AddControllers();

// Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new()
    {
        Title = "Simple ERP API",
        Version = "v1",
        Description = "API do Simple ERP — módulo Parceiros Comerciais."
    });
});

var app = builder.Build();

// O banco é criado/evoluído pelas migrations (não por EnsureCreated), de modo que o
// mesmo caminho vale para desenvolvimento, containers e produção.
await app.AplicarMigracoesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
