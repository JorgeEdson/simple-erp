using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Estoque
{
    public sealed class RegistrarMovimentacaoDeEstoqueUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISaldoDeEstoqueRepository _saldosRepository;
        private readonly IMovimentacaoDeEstoqueRepository _movimentacoesRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly RegistrarMovimentacaoDeEstoqueUseCase _useCase;

        public RegistrarMovimentacaoDeEstoqueUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _movimentacoesRepository = Substitute.For<IMovimentacaoDeEstoqueRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);
            _unitOfWork.MovimentacoesDeEstoqueRepository.Returns(_movimentacoesRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new RegistrarMovimentacaoDeEstoqueUseCase(_unitOfWork, _logService);
        }

        private void ConfigurarPersistencia()
        {
            _saldosRepository.AdicionarAsync(Arg.Any<SaldoDeEstoque>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.AtualizarAsync(Arg.Any<SaldoDeEstoque>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _movimentacoesRepository.AdicionarAsync(Arg.Any<MovimentacaoDeEstoque>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));
        }

        private static RegistrarMovimentacaoDeEstoqueEntrada Entrada(
            TipoDeMovimentacao tipo,
            decimal quantidade,
            bool permitirNegativo = false) =>
            new(
                IdProduto: 202604020001,
                Tipo: tipo,
                Quantidade: quantidade,
                OrigemTipo: TipoOrigemMovimentacao.Compra,
                OrigemIdReferencia: 500,
                PermitirSaldoNegativo: permitirNegativo);

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoExistir()
        {
            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(Entrada(TipoDeMovimentacao.EntradaPorCompra, 5m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");

            await _movimentacoesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<MovimentacaoDeEstoque>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveCriarSaldoERegistrarEntrada_QuandoNaoHouverSaldoAinda()
        {
            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));
            ConfigurarPersistencia();

            var resultado = await _useCase.ExecutarAsync(Entrada(TipoDeMovimentacao.EntradaPorCompra, 5m));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.SaldoResultante.Should().Be(5m);
            resultado.Instancia.Sentido.Should().Be("Entrada");

            await _saldosRepository
                .Received(1)
                .AdicionarAsync(Arg.Is<SaldoDeEstoque>(s => s.QuantidadeAtual == 5m), Arg.Any<CancellationToken>());
            await _saldosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<SaldoDeEstoque>(), Arg.Any<CancellationToken>());
            await _movimentacoesRepository
                .Received(1)
                .AdicionarAsync(Arg.Any<MovimentacaoDeEstoque>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaldoForInsuficiente()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(3m).Criar();

            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ObterPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(saldo));

            var resultado = await _useCase.ExecutarAsync(Entrada(TipoDeMovimentacao.SaidaPorVenda, 5m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("SALDO_INSUFICIENTE");

            await _movimentacoesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<MovimentacaoDeEstoque>(), Arg.Any<CancellationToken>());
            await _unitOfWork
                .DidNotReceive()
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRegistrarSaida_QuandoHouverSaldoSuficiente()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(10m).Criar();

            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ObterPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(saldo));
            ConfigurarPersistencia();

            var resultado = await _useCase.ExecutarAsync(Entrada(TipoDeMovimentacao.SaidaPorVenda, 4m));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.SaldoResultante.Should().Be(6m);
            resultado.Instancia.Sentido.Should().Be("Saida");

            await _saldosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<SaldoDeEstoque>(s => s.QuantidadeAtual == 6m), Arg.Any<CancellationToken>());
            await _saldosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<SaldoDeEstoque>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DevePermitirSaldoNegativo_QuandoConfigurado()
        {
            var saldo = SaldoDeEstoqueBuilder.Novo().ComSaldoInicial(3m).Criar();

            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ExistePorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ObterPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(saldo));
            ConfigurarPersistencia();

            var resultado = await _useCase.ExecutarAsync(
                Entrada(TipoDeMovimentacao.SaidaPorProducao, 5m, permitirNegativo: true));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.SaldoResultante.Should().Be(-2m);
        }
    }
}
