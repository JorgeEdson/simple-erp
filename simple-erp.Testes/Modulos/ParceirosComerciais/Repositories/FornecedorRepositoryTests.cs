using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Infraestrutura.Repositorios.ParceirosComerciais;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.Repositories
{
    /// <summary>
    /// A lógica compartilhada é coberta a fundo em ClienteRepositoryTests; aqui
    /// valida-se o que é próprio do Fornecedor: a tabela/mapeamento e o CNPJ.
    /// </summary>
    [Trait("Categoria", "Integracao")]
    public sealed class FornecedorRepositoryTests
        : IClassFixture<PostgresParceirosFixture>, IAsyncLifetime
    {
        // CNPJs válidos.
        private const string CnpjA = "11222333000181";
        private const string CnpjB = "11444777000161";

        private readonly PostgresParceirosFixture _fixture;

        public FornecedorRepositoryTests(PostgresParceirosFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirFornecedorComCnpj()
        {
            var fornecedor = FornecedorBuilder.Novo()
                .ComId(202607210100)
                .ComDocumento(CnpjA)
                .Criar();

            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new FornecedorRepository(contexto);
                (await repositorio.AdicionarAsync(fornecedor)).EhSucesso.Should().BeTrue();
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var repositorioLeitura = new FornecedorRepository(contextoLeitura);

            var recuperado = (await repositorioLeitura.ObterPorIdAsync(
                Id.TentarCriar(202607210100).Instancia)).Instancia;

            recuperado.Should().NotBeNull();
            recuperado!.Documento.Valor.Should().Be(CnpjA);
            recuperado.Documento.EhCnpj.Should().BeTrue("o Documento deve se re-hidratar como CNPJ");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveNormalizarDocumentoFormatadoNoFiltro()
        {
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new FornecedorRepository(contexto);
                await repositorio.AdicionarAsync(
                    FornecedorBuilder.Novo().ComId(202607210110).ComDocumento(CnpjA).Criar());
                await repositorio.AdicionarAsync(
                    FornecedorBuilder.Novo().ComId(202607210111).ComDocumento(CnpjB).Criar());
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var repositorioLeitura = new FornecedorRepository(contextoLeitura);

            // Usuário digita formatado; o repositório filtra pelos dígitos.
            var resultado = (await repositorioLeitura.ListarPaginadoAsync(
                1, 10, new ListarFornecedoresFiltros(Documento: "11.222.333/0001-81"))).Instancia;

            resultado.TotalRegistros.Should().Be(1);
            resultado.Itens.Should().ContainSingle(f => f.Documento.Valor == CnpjA);
        }

        [Fact]
        public async Task ExistePorDocumentoAsync_DeveSepararClientesDeFornecedores()
        {
            // Mesmo documento não colide entre tabelas: são agregados independentes.
            await using (var contexto = _fixture.CriarContexto())
            {
                var fornecedores = new FornecedorRepository(contexto);
                await fornecedores.AdicionarAsync(
                    FornecedorBuilder.Novo().ComId(202607210120).ComDocumento(CnpjA).Criar());
                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var clientes = new ClienteRepository(contextoLeitura);

            var documento = Documento.TentarCriar(CnpjA).Instancia;
            (await clientes.ExistePorDocumentoAsync(documento)).Instancia
                .Should().BeFalse("o CNPJ do fornecedor não deve aparecer na tabela de clientes");
        }
    }
}
