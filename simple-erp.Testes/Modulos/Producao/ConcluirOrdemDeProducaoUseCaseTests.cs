using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Eventos;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class ConcluirOrdemDeProducaoUseCaseTests
    {
        private const long IdOrdem = 202604020400;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrdemDeProducaoRepository _ordensRepository;
        private readonly ILogService _logService;
        private readonly IDispatcherDeEventos _dispatcher;
        private readonly ConcluirOrdemDeProducaoUseCase _useCase;

        public ConcluirOrdemDeProducaoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _ordensRepository = Substitute.For<IOrdemDeProducaoRepository>();
            _logService = Substitute.For<ILogService>();
            _dispatcher = Substitute.For<IDispatcherDeEventos>();

            _unitOfWork.OrdensDeProducaoRepository.Returns(_ordensRepository);
            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _useCase = new ConcluirOrdemDeProducaoUseCase(_unitOfWork, _logService, _dispatcher);
        }

        private void RetornarOrdem(OrdemDeProducao ordem) =>
            _ordensRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<OrdemDeProducao?>.Sucesso(ordem));

        private void ConfigurarPersistenciaOk()
        {
            _ordensRepository
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoOrdemNaoEstiverConfirmada()
        {
            RetornarOrdem(OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Criada().Criar());

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");

            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveConcluirEDespacharEvento_QuandoConfirmada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo()
                .ComId(IdOrdem)
                .SemNecessidades()
                .ComNecessidade(202604020010, 10m)
                .ComNecessidade(202604020011, 15m)
                .Confirmada()
                .Criar();

            RetornarOrdem(ordem);
            ConfigurarPersistenciaOk();

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");

            await _ordensRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<OrdemDeProducao>(o => o.EstaConcluida), Arg.Any<CancellationToken>());

            // O evento OrdemDeProducaoConcluida é despachado após a persistência,
            // carregando os insumos consumidos para as movimentações do Estoque.
            await _dispatcher
                .Received(1)
                .DespacharAsync(
                    Arg.Is<IEnumerable<EventoDeDominio>>(eventos =>
                        eventos.OfType<OrdemDeProducaoConcluida>()
                            .Any(e => e.InsumosConsumidos.Count == 2)),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarSucesso_MesmoQuandoHandlerFalhar()
        {
            // Consistência eventual: falha de handler não desfaz a conclusão persistida.
            RetornarOrdem(OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Confirmada().Criar());
            ConfigurarPersistenciaOk();

            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("FALHA_EM_HANDLER"));

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");
        }

        [Fact]
        public async Task ExecutarAsync_NaoDeveDespachar_QuandoSaveChangesFalhar()
        {
            RetornarOrdem(OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Confirmada().Criar());

            _ordensRepository
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();

            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }
    }
}
