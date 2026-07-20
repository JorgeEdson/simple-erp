using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Financeiro
{
    public sealed class BaixarTituloUseCaseTests
    {
        private const long IdTitulo = 202604020600;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITituloRepository _titulosRepository;
        private readonly ILogService _logService;
        private readonly BaixarTituloUseCase _useCase;

        public BaixarTituloUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _titulosRepository = Substitute.For<ITituloRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.TitulosRepository.Returns(_titulosRepository);

            _useCase = new BaixarTituloUseCase(_unitOfWork, _logService);
        }

        private void RetornarTitulo(Titulo titulo) =>
            _titulosRepository.ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<Titulo?>.Sucesso(titulo));

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoValorExcederSaldo()
        {
            RetornarTitulo(TituloBuilder.Novo().ComId(IdTitulo).ComValorOriginal(100m).Criar());

            var resultado = await _useCase.ExecutarAsync(new BaixarTituloEntrada(IdTitulo, 150m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("VALOR_BAIXA_EXCEDE_SALDO");

            await _titulosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<Titulo>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveBaixarParcialmente_QuandoValorForValido()
        {
            RetornarTitulo(TituloBuilder.Novo().ComId(IdTitulo).ComValorOriginal(100m).Criar());

            _titulosRepository.AtualizarAsync(Arg.Any<Titulo>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new BaixarTituloEntrada(IdTitulo, 40m));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("ParcialmenteBaixado");
            resultado.Instancia.ValorBaixado.Should().Be(40m);
            resultado.Instancia.SaldoDevedor.Should().Be(60m);

            await _titulosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<Titulo>(t => t.EstaParcialmenteBaixado), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveLiquidar_QuandoBaixaZerarSaldo()
        {
            RetornarTitulo(TituloBuilder.Novo().ComId(IdTitulo).ComValorOriginal(100m).ComBaixaInicial(70m).Criar());

            _titulosRepository.AtualizarAsync(Arg.Any<Titulo>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new BaixarTituloEntrada(IdTitulo, 30m));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Liquidado");
            resultado.Instancia.SaldoDevedor.Should().Be(0m);
        }
    }
}
