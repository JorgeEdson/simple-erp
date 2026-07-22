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
        private readonly AprovarPedidoDeVendaUseCase _useCase;

        public AprovarPedidoDeVendaUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeVendaRepository>();
            _saldosRepository = Substitute.For<ISaldoDeEstoqueRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeVendaRepository.Returns(_pedidosRepository);
            _unitOfWork.SaldosDeEstoqueRepository.Returns(_saldosRepository);

            _useCase = new AprovarPedidoDeVendaUseCase(_unitOfWork, _logService);
        }

        /// <summary>Devolve o agregado para que o teste possa inspecionar seus eventos.</summary>
        private PedidoDeVenda RetornarPedidoEmEdicao()
        {
            var pedido = PedidoDeVendaBuilder.Novo().ComId(IdPedido).EmEdicao().Criar();
            _pedidosRepository.ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeVenda?>.Sucesso(pedido));

            return pedido;
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
            var pedido = RetornarPedidoEmEdicao();
            ConfigurarSaldo(1m); // precisa de 2

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ESTOQUE_INSUFICIENTE");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>());

            pedido.EventosDeDominio.OfType<PedidoDeVendaAprovado>().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutarAsync_DeveAprovarERegistrarEvento_QuandoHouverSaldo()
        {
            var pedido = RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);
            ConfigurarPersistenciaOk();

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Aprovado");

            await _pedidosRepository
                .Received(1)
                .AtualizarAsync(Arg.Is<PedidoDeVenda>(p => p.EstaAprovado), Arg.Any<CancellationToken>());

            // O evento carrega os itens que o Estoque usará para a saída por venda e o
            // Financeiro para o título a receber. Ele fica no agregado; o transporte até
            // a caixa de saída é responsabilidade do interceptor de persistência.
            pedido.EventosDeDominio
                .OfType<PedidoDeVendaAprovado>()
                .Should().ContainSingle(evento =>
                    evento.IdPedidoDeVenda.Valor == IdPedido && evento.Itens.Count > 0);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            RetornarPedidoEmEdicao();
            ConfigurarSaldo(10m);

            _pedidosRepository.AtualizarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(new AprovarPedidoDeVendaEntrada(IdPedido));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");
        }
    }
}
