using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class EfetivarPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly ILogService _logService;
        private readonly EfetivarPedidoDeCompraUseCase _useCase;

        public EfetivarPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);

            _useCase = new EfetivarPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoPedidoNaoEstiverAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar(); // em edição

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_APROVADO_NAO_PODE_SER_EFETIVADO");

            await _pedidosRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());

            pedido.EventosDeDominio.OfType<PedidoDeCompraEfetivado>().Should().BeEmpty();
        }

        [Fact]
        public async Task ExecutarAsync_DeveEfetivarERegistrarEvento_QuandoPedidoEstiverAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Concluida");

            // O use case não despacha mais nada: ele produz o evento e o deixa no
            // agregado. Quem o transporta para a caixa de saída é o interceptor de
            // persistência, dentro da mesma transação do SaveChanges — e é justamente
            // por ser infraestrutura que ele não participa deste teste de unidade.
            pedido.EventosDeDominio
                .OfType<PedidoDeCompraEfetivado>()
                .Should().ContainSingle();
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            _pedidosRepository
                .ObterPorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<PedidoDeCompra?>.Sucesso(pedido));

            _pedidosRepository
                .AtualizarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(new EfetivarPedidoDeCompraEntrada(pedido.Id.Valor));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");
        }
    }
}
