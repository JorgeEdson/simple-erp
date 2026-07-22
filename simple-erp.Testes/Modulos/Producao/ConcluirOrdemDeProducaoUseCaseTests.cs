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
        private readonly ConcluirOrdemDeProducaoUseCase _useCase;

        public ConcluirOrdemDeProducaoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _ordensRepository = Substitute.For<IOrdemDeProducaoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.OrdensDeProducaoRepository.Returns(_ordensRepository);

            _useCase = new ConcluirOrdemDeProducaoUseCase(_unitOfWork, _logService);
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
            var ordem = OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Criada().Criar();
            RetornarOrdem(ordem);

            var resultado = await _useCase.ExecutarAsync(new ConcluirOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");

            ordem.EventosDeDominio.OfType<OrdemDeProducaoConcluida>().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutarAsync_DeveConcluirERegistrarEvento_QuandoConfirmada()
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

            // O evento carrega os insumos consumidos, que o Estoque usará para gerar as
            // movimentações. Ele fica no agregado até o interceptor gravá-lo na caixa de
            // saída — o use case não o entrega a ninguém.
            ordem.EventosDeDominio
                .OfType<OrdemDeProducaoConcluida>()
                .Should().ContainSingle(evento => evento.InsumosConsumidos.Count == 2);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
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
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");
        }
    }
}
