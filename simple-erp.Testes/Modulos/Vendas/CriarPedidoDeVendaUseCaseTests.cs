using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Vendas.UseCases;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Vendas
{
    public sealed class CriarPedidoDeVendaUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeVendaRepository _pedidosRepository;
        private readonly IClienteRepository _clientesRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly CriarPedidoDeVendaUseCase _useCase;

        public CriarPedidoDeVendaUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeVendaRepository>();
            _clientesRepository = Substitute.For<IClienteRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeVendaRepository.Returns(_pedidosRepository);
            _unitOfWork.ClientesRepository.Returns(_clientesRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new CriarPedidoDeVendaUseCase(_unitOfWork, _logService);
        }

        private static CriarPedidoDeVendaEntrada EntradaValida() =>
            new(202604020002, new List<ItemPedidoDeVendaEntrada> { new(202604020001, 2m, 10.00m) });

        private void ConfigurarExistencias(bool cliente = true, bool produto = true)
        {
            _clientesRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(cliente));
            _produtosRepository.ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(produto));
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoClienteNaoExistir()
        {
            ConfigurarExistencias(cliente: false);

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("CLIENTE_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoExistir()
        {
            ConfigurarExistencias(cliente: true, produto: false);

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");
        }

        [Fact]
        public async Task ExecutarAsync_DeveCriarPedidoComNumeroSequencial_QuandoDadosForemValidos()
        {
            ConfigurarExistencias();

            _pedidosRepository.ObterProximoNumeroAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(42));
            _pedidosRepository.AdicionarAsync(Arg.Any<PedidoDeVenda>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Numero.Should().Be(42);
            resultado.Instancia.Status.Should().Be("EmEdicao");
            resultado.Instancia.ValorTotal.Should().Be(20.00m);

            await _pedidosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<PedidoDeVenda>(p => p.Numero == 42 && p.EstaEmEdicao && p.Itens.Count == 1),
                    Arg.Any<CancellationToken>());
        }
    }
}
