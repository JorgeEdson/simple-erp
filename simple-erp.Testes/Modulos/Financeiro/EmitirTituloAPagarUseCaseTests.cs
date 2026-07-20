using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Financeiro.UseCases;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using System;

namespace simple_erp.Testes.Modulos.Financeiro
{
    public sealed class EmitirTituloAPagarUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITituloRepository _titulosRepository;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly ILogService _logService;
        private readonly EmitirTituloAPagarUseCase _useCase;

        public EmitirTituloAPagarUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _titulosRepository = Substitute.For<ITituloRepository>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.TitulosRepository.Returns(_titulosRepository);
            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);

            _useCase = new EmitirTituloAPagarUseCase(_unitOfWork, _logService);
        }

        private static EmitirTituloAPagarEntrada EntradaValida() =>
            new(202604020002, 100.00m, DateTime.UtcNow.AddDays(30), 202604020003);

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoFornecedorNaoExistir()
        {
            _fornecedoresRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_NAO_ENCONTRADO");

            await _titulosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<Titulo>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveEmitirTituloAPagar_QuandoDadosForemValidos()
        {
            _fornecedoresRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _titulosRepository.AdicionarAsync(Arg.Any<Titulo>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Tipo.Should().Be("APagar");
            resultado.Instancia.Status.Should().Be("EmAberto");
            resultado.Instancia.ValorOriginal.Should().Be(100.00m);
            resultado.Instancia.SaldoDevedor.Should().Be(100.00m);

            await _titulosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<Titulo>(t => t.EhAPagar && t.EstaEmAberto && t.ValorOriginal == 100.00m),
                    Arg.Any<CancellationToken>());
        }
    }
}
