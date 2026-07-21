using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Infraestrutura.Persistencia;
using simple_erp.Infraestrutura.Persistencia.Contexto;
using simple_erp.Infraestrutura.Repositorios.CatalogoDeProdutos;
using simple_erp.Infraestrutura.Repositorios.Estoque;
using simple_erp.Infraestrutura.Repositorios.Financeiro;
using simple_erp.Infraestrutura.Repositorios.ParceirosComerciais;
using simple_erp.Infraestrutura.Repositorios.Producao;
using simple_erp.Infraestrutura.Repositorios.Suprimentos;
using simple_erp.Infraestrutura.Repositorios.Vendas;

namespace simple_erp.Infraestrutura.Extensoes
{   
    public static class InjecaoDeDependenciaExtensions
    {
        public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<SimpleErpDbContext>(options =>
                options
                    .UseNpgsql(connectionString)                    
                    .UseSnakeCaseNamingConvention());
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Módulo Parceiros Comerciais.
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IFornecedorRepository, FornecedorRepository>();

            // Módulo Catálogo de Produtos.
            services.AddScoped<IProdutoRepository, ProdutoRepository>();

            // Módulo Estoque.
            services.AddScoped<ISaldoDeEstoqueRepository, SaldoDeEstoqueRepository>();
            services.AddScoped<IMovimentacaoDeEstoqueRepository, MovimentacaoDeEstoqueRepository>();

            // Módulo Financeiro.
            services.AddScoped<ITituloRepository, TituloRepository>();

            // Módulo Suprimentos.
            services.AddScoped<IPedidoDeCompraRepository, PedidoDeCompraRepository>();

            // Módulo Produção (e subdomínio Composição).
            services.AddScoped<IOrdemDeProducaoRepository, OrdemDeProducaoRepository>();
            services.AddScoped<IComposicaoDeProdutoRepository, ComposicaoDeProdutoRepository>();

            // Módulo Vendas.
            services.AddScoped<IPedidoDeVendaRepository, PedidoDeVendaRepository>();

            return services;
        }
    }
}
