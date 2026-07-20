using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class ConcluirOrdemDeProducaoUseCaseTests
    {
        private const long IdOrdem = 202604020400;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrdemDeProducaoRepository _ordensRepository;
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;
        private readonly ILogService _logService;
        private readonly ConcluirOrdemDeProducaoUseCase _useCase;

        public ConcluirOrdemDeProducaoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _ordensRepository = Substitute.For<IOrdemDeProducaoRepository>();
            _registrarMovimentacao = Substitute.For<IRegistrarMovimentacaoDeEstoqueUseCase>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.OrdensDeProducaoRepository.Returns(_ordensRepository);

            _useCase = new ConcluirOrdemDeProducaoUseCase(_unitOfWork, _logService, _registrarMovimentacao);
        }

        private static Resultado<RegistrarMovimentacaoDeEstoqueSaida> MovimentacaoOk() =>
            Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                new RegistrarMovimentacaoDeEstoqueSaida(1, 1, "EntradaPorProducao", "Entrada", 1m, 1m));

        private void RetornarOrdem(OrdemDeProducao ordem) =>
            _ordensRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<OrdemDeProducao?>.Sucesso(ordem));

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOrdemNaoEstiverConfirmada()
        {
            RetornarOrdem(OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Criada().Criar());

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");

            await _registrarMovimentacao
                .DidNotReceive()
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoBaixaDeInsumoFalhar()
        {
            RetornarOrdem(OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Confirmada().Criar());

            _registrarMovimentacao
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha("SALDO_INSUFICIENTE"));

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SALDO_INSUFICIENTE");

            await _ordensRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveConcluirEGerarMovimentacoes_QuandoConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo()
                .ComId(IdOrdem)
                .SemNecessidades()
                .ComNecessidade(202604020010, 10m)
                .ComNecessidade(202604020011, 15m)
                .Confirmada()
                .Criar();

            RetornarOrdem(ordem);

            _registrarMovimentacao
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(MovimentacaoOk());
            _ordensRepository
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");

            // 2 saídas de matéria-prima + 1 entrada do produto acabado
            await _registrarMovimentacao
                .Received(3)
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>());

            await _registrarMovimentacao
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<RegistrarMovimentacaoDeEstoqueEntrada>(e => e.Tipo == TipoDeMovimentacao.EntradaPorProducao),
                    Arg.Any<CancellationToken>());

            await _ordensRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<OrdemDeProducao>(o => o.EstaConcluida), Arg.Any<CancellationToken>());
        }
    }
}
