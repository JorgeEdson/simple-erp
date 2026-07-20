using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class ConfirmarOrdemDeProducaoUseCaseTests
    {
        private const long IdOrdem = 202604020400;
        private const long IdInsumoA = 202604020010; // necessidade 10
        private const long IdInsumoB = 202604020011; // necessidade 15

        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrdemDeProducaoRepository _ordensRepository;
        private readonly ISaldoDeEstoqueRepository _saldosRepository;
        private readonly ILogService _logService;
        private readonly ConfirmarOrdemDeProducaoUseCase _useCase;

        public ConfirmarOrdemDeProducaoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _ordensRepository = Substitute.For<IOrdemDeProducaoRepository>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.OrdensDeProducaoRepository.Returns(_ordensRepository);
            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);

            _useCase = new ConfirmarOrdemDeProducaoUseCase(_unitOfWork, _logService);
        }

        private void RetornarOrdemCriada()
        {
            var ordem = OrdemDeProducaoBuilder.Novo().ComId(IdOrdem).Criada().Criar();
            _ordensRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<OrdemDeProducao?>.Sucesso(ordem));
        }

        private void ConfigurarSaldo(long idInsumo, decimal saldo)
        {
            _saldosRepository
                .ExistePorProdutoAsync(Arg.Is<Id>(i => i.Valor == idInsumo), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository
                .ObterPorProdutoAsync(Arg.Is<Id>(i => i.Valor == idInsumo), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(
                    SaldoDeEstoqueBuilder.Novo().ComIdProduto(idInsumo).ComSaldoInicial(saldo).Criar()));
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoEstoqueForInsuficiente()
        {
            RetornarOrdemCriada();
            ConfigurarSaldo(IdInsumoA, 5m);   // precisa de 10
            ConfigurarSaldo(IdInsumoB, 100m); // suficiente

            var resultado = await _useCase.ExecutarAsync(new ConfirmarOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ESTOQUE_INSUFICIENTE");
            resultado.Erros.Should().Contain(e => e.Contains($"IdInsumo={IdInsumoA}"));

            await _ordensRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveConfirmar_QuandoHouverEstoqueSuficiente()
        {
            RetornarOrdemCriada();
            ConfigurarSaldo(IdInsumoA, 10m);
            ConfigurarSaldo(IdInsumoB, 15m);

            _ordensRepository
                .AtualizarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new ConfirmarOrdemDeProducaoEntrada(IdOrdem));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Confirmada");

            await _ordensRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<OrdemDeProducao>(o => o.EstaConfirmada), Arg.Any<CancellationToken>());
        }
    }
}
