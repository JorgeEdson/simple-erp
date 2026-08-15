using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class ConsultarSaldoDeEstoqueUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISaldoDeEstoqueRepository _saldosRepository;
        private readonly ILogService _logService;
        private readonly ConsultarSaldoDeEstoqueUseCase _useCase;

        public ConsultarSaldoDeEstoqueUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);

            _useCase = new ConsultarSaldoDeEstoqueUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoIdForInvalido()
        {
            var resultado = await _useCase.ExecutarAsync(new ConsultarSaldoDeEstoqueEntrada(Guid.Empty));

            resultado.EhFalha.Should().BeTrue();

            await _saldosRepository
                .DidNotReceive()
                .ExistePorProdutoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarZero_QuandoProdutoNaoTiverSaldo()
        {
            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(new ConsultarSaldoDeEstoqueEntrada(new Guid("00000000-0000-0000-0000-202604020001")));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.QuantidadeAtual.Should().Be(0m);
            resultado.Instancia.PossuiRegistroDeSaldo.Should().BeFalse();

            await _saldosRepository
                .DidNotReceive()
                .ObterPorProdutoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarSaldoAtual_QuandoProdutoTiverSaldo()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(12m).Criar();

            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ObterPorProdutoAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(saldo));

            var resultado = await _useCase.ExecutarAsync(new ConsultarSaldoDeEstoqueEntrada(new Guid("00000000-0000-0000-0000-202604020001")));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.QuantidadeAtual.Should().Be(12m);
            resultado.Instancia.PossuiRegistroDeSaldo.Should().BeTrue();
        }
    }
}
