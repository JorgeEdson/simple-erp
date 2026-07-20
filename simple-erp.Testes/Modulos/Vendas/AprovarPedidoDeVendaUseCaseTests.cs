using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Vendas
{
    public sealed class AprovarPedidoDeVendaUseCaseTests
    {
        private const long IdPedido = 202604020500;
        private const long IdProduto = 202604020001; // builder: quantidade 2

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeVendaRepository _pedidosRepository;
        private readonly ISaldoDeEstoqueRepository _saldosRepository;
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;
        private readonly ILogService _logService;
        private readonly AprovarPedidoDeVendaUseCase _useCase;

        public AprovarPedidoDeVendaUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeVendaRepository>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _registrarMovimentacao = Substitute.For<IRegistrarMovimentacaoDeEstoqueUseCase>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeVendaRepository.Returns(_pedidosRepository);
            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);

            _useCase = new AprovarPedidoDeVendaUseCase(_unitOfWork, _logService, _registrarMovimentacao);
        }

        private void RetornarPedidoEmEdicao()
        {
            var pedido = PedidoDeVendaBuilder.Novo().ComId(IdPedido).EmEdicao().Criar();
            _pedidosRepository.ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeVenda?>.Sucesso(pedido));
        }

        private void ConfigurarSaldo(decimal saldo)
        {
            _saldosRepository.ExistePorProdutoAsync(Arg.Is<Id>(i => i.Valor == IdProduto), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _saldosRepository.ObterPorProdutoAsync(Arg.Is<Id>(i => i.Valor == IdProduto), Arg.Any<CancellationToken>())
                .Returns(Resultado<SaldoDeEstoque?>.Sucesso(
                    SaldoDeEstoqueBuilder.Novo().ComIdProduto(IdProduto).ComSaldoInicial(saldo).Criar()));
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoEstoqueForInsuficiente()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(1m); // precisa de 2

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ESTOQUE_INSUFICIENTE");

            await _registrarMovimentacao
                .DidNotReceive()
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>());
            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveAprovarEDarBaixaNoEstoque_QuandoHouverSaldo()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);

            _registrarMovimentacao
                .ExecutarAsync(Arg.Any<RegistrarMovimentacaoDeEstoqueEntrada>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                    new RegistrarMovimentacaoDeEstoqueSaida(1, IdProduto, "SaidaPorVenda", "Saida", 2m, 8m)));
            _pedidosRepository.AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Aprovado");

            await _registrarMovimentacao
                .Received(1)
                .ExecutarAsync(
                    Arg.Is<RegistrarMovimentacaoDeEstoqueEntrada>(e =>
                        e.Tipo == TipoDeMovimentacao.SaidaPorVenda &&
                        e.IdProduto == IdProduto &&
                        e.Quantidade == 2m),
                    Arg.Any<CancellationToken>());

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeVenda>(p => p.EstaAprovado), Arg.Any<CancellationToken>());
        }
    }
}
