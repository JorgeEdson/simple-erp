using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.Eventos;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Modulos.Vendas
{
    public sealed class AprovarPedidoDeVendaUseCaseTests
    {
        private const long IdPedido = 202604020500;
        private const long IdProduto = 202604020001; // builder: quantidade 2

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeVendaRepository _pedidosRepository;
        private readonly ISaldoDeEstoqueRepository _saldosRepository;
        private readonly ILogService _logService;
        private readonly IDispatcherDeEventos _dispatcher;
        private readonly AprovarPedidoDeVendaUseCase _useCase;

        public AprovarPedidoDeVendaUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeVendaRepository>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _logService = Substitute.For<ILogService>();
            _dispatcher = Substitute.For<IDispatcherDeEventos>();

            _unitOfWork.PedidosDeVendaRepository.Returns(_pedidosRepository);
            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);
            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _useCase = new AprovarPedidoDeVendaUseCase(_unitOfWork, _logService, _dispatcher);
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

        private void ConfigurarPersistenciaOk()
        {
            _pedidosRepository.AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoEstoqueForInsuficiente()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(1m); // precisa de 2

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ESTOQUE_INSUFICIENTE");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>());
            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveAprovarEDespacharEvento_QuandoHouverSaldo()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);
            ConfigurarPersistenciaOk();

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Aprovado");

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeVenda>(p => p.EstaAprovado), Arg.Any<CancellationToken>());

            // O evento PedidoDeVendaAprovado é despachado após a persistência: o Estoque
            // reage com a saída por venda e o Financeiro com o título a receber.
            await _dispatcher
                .Received(1)
                .DespacharAsync(
                    Arg.Is<IEnumerable<EventoDeDominio>>(eventos =>
                        eventos.OfType<PedidoDeVendaAprovado>()
                            .Any(e => e.IdPedidoDeVenda.Valor == IdPedido && e.Itens.Count > 0)),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarSucesso_MesmoQuandoHandlerFalhar()
        {
            // Consistência eventual: falha de handler não desfaz a aprovação persistida.
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);
            ConfigurarPersistenciaOk();

            _dispatcher
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Falha("FALHA_EM_HANDLER"));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Aprovado");
        }

        [Fact]
        public async Task ExecutarAsync_NaoDeveDespachar_QuandoSaveChangesFalhar()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);

            _pedidosRepository.AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhFalha.Should().BeTrue();

            await _dispatcher
                .DidNotReceive()
                .DespacharAsync(Arg.Any<IEnumerable<EventoDeDominio>>(), Arg.Any<CancellationToken>());
        }
    }
}
