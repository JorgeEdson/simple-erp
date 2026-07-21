using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.UseCases;
using simple_erp.Infraestrutura.Repositorios.ParceirosComerciais;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.Repositories
{
    [Trait("Categoria", "Integracao")]
    public sealed class ClienteRepositoryTests
        : IClassFixture<PostgresParceirosFixture>, IAsyncLifetime
    {
        // CPFs válidos distintos para não colidir com o índice único de documento.
        private const string CpfA = "12345678909";
        private const string CpfB = "52998224725";
        private const string CpfC = "11144477735";

        private readonly PostgresParceirosFixture _fixture;

        public ClienteRepositoryTests(PostgresParceirosFixture fixture)
        {
            _fixture = fixture;
        }

        public Task InitializeAsync() => _fixture.LimparAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private async Task SalvarAsync(params Cliente[] clientes)
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);

            foreach (var cliente in clientes)
            {
                var resultado = await repositorio.AdicionarAsync(cliente);
                resultado.EhSucesso.Should().BeTrue();
            }

            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task AdicionarEObterPorId_DevePersistirERecuperarTodosOsValueObjects()
        {
            var endereco = EnderecoBuilder.Novo()
                .ComCidade("São Paulo")
                .ComEstado("SP")
                .Criar();

            var cliente = ClienteBuilder.Novo()
                .ComId(202607210001)
                .ComNome("Maria da Silva")
                .ComDocumento(CpfA)
                .ComEmail("maria@teste.com")
                .ComEndereco(endereco)
                .Criar();

            await SalvarAsync(cliente);

            // Contexto novo: a leitura vem do banco, exercitando todos os conversores.
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(Id.TentarCriar(202607210001).Instancia);

            resultado.EhSucesso.Should().BeTrue();
            var recuperado = resultado.Instancia;
            recuperado.Should().NotBeNull();
            recuperado!.Id.Valor.Should().Be(202607210001);
            recuperado.Nome.Valor.Should().Be("Maria da Silva");
            recuperado.Documento.Valor.Should().Be(CpfA);
            recuperado.Documento.EhCpf.Should().BeTrue("o Documento deve se re-hidratar como CPF a partir dos dígitos");
            recuperado.Email.Valor.Should().Be("maria@teste.com");
            recuperado.Endereco.Cidade.Should().Be("São Paulo", "o endereço deve voltar íntegro do jsonb");
            recuperado.Endereco.Estado.Should().Be("SP");
            recuperado.Ativo.Should().BeTrue();
        }

        [Fact]
        public async Task ObterPorIdAsync_DeveRetornarSucessoComNulo_QuandoNaoEncontrado()
        {
            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);

            var resultado = await repositorio.ObterPorIdAsync(Id.TentarCriar(999999999999).Instancia);

            // Contrato do repositório: não encontrar não é falha de infraestrutura.
            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().BeNull();
        }

        [Fact]
        public async Task ObterPorDocumentoAsync_DeveEncontrarPeloValorCanonico()
        {
            await SalvarAsync(ClienteBuilder.Novo().ComId(202607210010).ComDocumento(CpfB).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);
            var documento = Documento.TentarCriar(CpfB).Instancia;

            var resultado = await repositorio.ObterPorDocumentoAsync(documento);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Should().NotBeNull();
            resultado.Instancia!.Id.Valor.Should().Be(202607210010);

            (await repositorio.ExistePorDocumentoAsync(documento)).Instancia.Should().BeTrue();
            (await repositorio.ExistePorDocumentoAsync(Documento.TentarCriar(CpfC).Instancia))
                .Instancia.Should().BeFalse();
        }

        [Fact]
        public async Task ExisteOutroPorDocumentoAsync_DeveIgnorarOProprioCliente()
        {
            await SalvarAsync(
                ClienteBuilder.Novo().ComId(202607210020).ComDocumento(CpfA).Criar(),
                ClienteBuilder.Novo().ComId(202607210021).ComDocumento(CpfB).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);
            var documento = Documento.TentarCriar(CpfA).Instancia;

            // O dono do documento não conta como "outro" — regra usada na edição.
            (await repositorio.ExisteOutroPorDocumentoAsync(
                Id.TentarCriar(202607210020).Instancia, documento)).Instancia.Should().BeFalse();

            (await repositorio.ExisteOutroPorDocumentoAsync(
                Id.TentarCriar(202607210021).Instancia, documento)).Instancia.Should().BeTrue();
        }

        [Fact]
        public async Task AtualizarAsync_DevePersistirAlteracoesDaEntidadeRastreada()
        {
            await SalvarAsync(ClienteBuilder.Novo().ComId(202607210030).ComDocumento(CpfA).Criar());

            // Fluxo real de edição: obter (rastreado) → alterar → Atualizar → SaveChanges.
            await using (var contexto = _fixture.CriarContexto())
            {
                var repositorio = new ClienteRepository(contexto);
                var cliente = (await repositorio.ObterPorIdAsync(
                    Id.TentarCriar(202607210030).Instancia)).Instancia!;

                cliente.AlterarNome(Nome.TentarCriar("Nome Editado").Instancia);
                (await repositorio.AtualizarAsync(cliente)).EhSucesso.Should().BeTrue();

                await contexto.SaveChangesAsync();
            }

            await using var contextoLeitura = _fixture.CriarContexto();
            var repositorioLeitura = new ClienteRepository(contextoLeitura);
            var recuperado = (await repositorioLeitura.ObterPorIdAsync(
                Id.TentarCriar(202607210030).Instancia)).Instancia!;

            recuperado.Nome.Valor.Should().Be("Nome Editado");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DeveFiltrarNoBanco_InclusiveDentroDoJsonb()
        {
            await SalvarAsync(
                ClienteBuilder.Novo().ComId(202607210040).ComNome("Maria Silva").ComDocumento(CpfA)
                    .ComEndereco(EnderecoBuilder.Novo().ComCidade("São Paulo").ComEstado("SP").Criar())
                    .Criar(),
                ClienteBuilder.Novo().ComId(202607210041).ComNome("Maria Souza").ComDocumento(CpfB)
                    .ComEndereco(EnderecoBuilder.Novo().ComCidade("Rio de Janeiro").ComEstado("RJ").Criar())
                    .Inativo()
                    .Criar(),
                ClienteBuilder.Novo().ComId(202607210042).ComNome("João Lima").ComDocumento(CpfC)
                    .ComEndereco(EnderecoBuilder.Novo().ComCidade("São Paulo").ComEstado("SP").Criar())
                    .Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);

            // Nome (ILIKE, case-insensitive)
            var porNome = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarClientesFiltros(Nome: "maria"))).Instancia;
            porNome.TotalRegistros.Should().Be(2);

            // Cidade — filtro resolvido DENTRO do jsonb pelo Postgres
            var porCidade = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarClientesFiltros(Cidade: "São Paulo"))).Instancia;
            porCidade.TotalRegistros.Should().Be(2);
            porCidade.Itens.Should().OnlyContain(c => c.Endereco.Cidade == "São Paulo");

            // Ativo
            var inativos = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarClientesFiltros(Ativo: false))).Instancia;
            inativos.TotalRegistros.Should().Be(1);
            inativos.Itens.Should().OnlyContain(c => !c.Ativo);

            // Filtros combinados
            var mariasAtivas = (await repositorio.ListarPaginadoAsync(
                1, 10, new ListarClientesFiltros(Nome: "Maria", Ativo: true))).Instancia;
            mariasAtivas.TotalRegistros.Should().Be(1);
            mariasAtivas.Itens.Should().ContainSingle(c => c.Nome.Valor == "Maria Silva");
        }

        [Fact]
        public async Task ListarPaginadoAsync_DevePaginarOrdenandoPorNome()
        {
            await SalvarAsync(
                ClienteBuilder.Novo().ComId(202607210050).ComNome("Carlos").ComDocumento(CpfA).Criar(),
                ClienteBuilder.Novo().ComId(202607210051).ComNome("Ana").ComDocumento(CpfB).Criar(),
                ClienteBuilder.Novo().ComId(202607210052).ComNome("Bruno").ComDocumento(CpfC).Criar());

            await using var contexto = _fixture.CriarContexto();
            var repositorio = new ClienteRepository(contexto);

            var pagina1 = (await repositorio.ListarPaginadoAsync(1, 2)).Instancia;
            pagina1.TotalRegistros.Should().Be(3);
            pagina1.TotalPaginas.Should().Be(2);
            pagina1.Itens.Select(c => c.Nome.Valor).Should().ContainInOrder("Ana", "Bruno");

            var pagina2 = (await repositorio.ListarPaginadoAsync(2, 2)).Instancia;
            pagina2.Itens.Should().ContainSingle(c => c.Nome.Valor == "Carlos");
        }
    }
}
