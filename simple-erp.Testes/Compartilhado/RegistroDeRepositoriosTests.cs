using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Infraestrutura.Extensoes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Compartilhado
{  
    public sealed class RegistroDeRepositoriosTests
    {
        
        private const string ConexaoFicticia =
            "Host=localhost;Database=inexistente;Username=teste;Password=teste";

        private static IServiceCollection ServicosDaAplicacao() =>
            new ServiceCollection().AdicionarInfraestrutura(ConexaoFicticia);

        /// <summary>Contratos de repositório declarados no domínio, lidos direto do Core.</summary>
        private static IReadOnlyList<Type> ContratosDeclaradosNoCore() =>
            typeof(IRepositorio).Assembly
                .GetTypes()
                .Where(tipo =>
                    tipo.IsInterface &&
                    tipo != typeof(IRepositorio) &&
                    typeof(IRepositorio).IsAssignableFrom(tipo))
                .OrderBy(tipo => tipo.Name)
                .ToList();

        public static TheoryData<Type> Contratos()
        {
            var dados = new TheoryData<Type>();

            foreach (var contrato in ContratosDeclaradosNoCore())
                dados.Add(contrato);

            return dados;
        }

        [Fact]
        public void OCoreDeveDeclararContratosDeRepositorio()
        {
            // Sem esta âncora, todos os testes abaixo passariam por vacuidade caso a
            // varredura deixasse de enxergar qualquer coisa.
            ContratosDeclaradosNoCore()
                .Should().NotBeEmpty("os repositórios do domínio precisam derivar de IRepositorio");
        }

        [Theory]
        [MemberData(nameof(Contratos))]
        public void TodoContratoDeRepositorioDeveTerImplementacaoRegistrada(Type contrato)
        {
            var registros = ServicosDaAplicacao()
                .Where(servico => servico.ServiceType == contrato)
                .ToList();

            registros.Should().ContainSingle(
                $"'{contrato.Name}' precisa de exatamente uma implementação registrada");

            var implementacao = registros[0].ImplementationType;

            implementacao.Should().NotBeNull();
            implementacao!.IsAbstract.Should().BeFalse();
            contrato.IsAssignableFrom(implementacao).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Contratos))]
        public void TodoRepositorioDeveSerScoped(Type contrato)
        {
            var registro = ServicosDaAplicacao().Single(servico => servico.ServiceType == contrato);

            // Singleton seguraria o DbContext da primeira requisição para sempre
            // (captive dependency); Transient daria a cada repositório um change tracker
            // próprio e quebraria a transação única por agregado.
            registro.Lifetime.Should().Be(
                ServiceLifetime.Scoped,
                $"'{contrato.Name}' compartilha o DbContext da requisição");
        }

        [Theory]
        [MemberData(nameof(Contratos))]
        public async Task TodoRepositorioDeveSerResolvivel(Type contrato)
        {
            // Registrar não é o bastante: este teste constrói o objeto de verdade e por
            // isso também pega dependências de construtor que ninguém registrou.
            // Escopo assíncrono pelo mesmo motivo do teste do UnitOfWork, logo abaixo.
            await using var provedor = ServicosDaAplicacao().BuildServiceProvider(validateScopes: true);
            await using var escopo = provedor.CreateAsyncScope();

            var repositorio = escopo.ServiceProvider.GetService(contrato);

            repositorio.Should().NotBeNull($"'{contrato.Name}' deve ser construível pelo container");
        }

        [Fact]
        public void AUnidadeDeTrabalhoDeveEstarRegistradaComoScoped()
        {
            // A unidade de trabalho fica fora da varredura (não é repositório), então
            // vale confirmar que a mudança para reflection não a deixou para trás.
            var registro = ServicosDaAplicacao()
                .SingleOrDefault(servico => servico.ServiceType == typeof(IUnitOfWork));

            registro.Should().NotBeNull("IUnitOfWork continua registrado explicitamente");
            registro!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public async Task AUnidadeDeTrabalhoDeveSerConstruivel()
        {   
            await using var provedor = ServicosDaAplicacao().BuildServiceProvider(validateScopes: true);
            await using var escopo = provedor.CreateAsyncScope();

            var acao = () => escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();

            acao.Should().NotThrow();
        }

        [Fact]
        public void OMarcadorNaoDeveSerRegistradoDiretamente()
        {
            // Registrar IRepositorio criaria uma resolução ambígua, sem utilidade.
            ServicosDaAplicacao()
                .Any(servico => servico.ServiceType == typeof(IRepositorio))
                .Should().BeFalse();
        }
    }
}
