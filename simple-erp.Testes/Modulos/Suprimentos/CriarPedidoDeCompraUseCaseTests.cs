using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.ParceirosComerciais.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Suprimentos.UseCases;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class CriarPedidoDeCompraUseCaseTests
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPedidoDeCompraRepository _pedidosRepository;
        private readonly IFornecedorRepository _fornecedoresRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly CriarPedidoDeCompraUseCase _useCase;

        public CriarPedidoDeCompraUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _pedidosRepository = Substitute.For<IPedidoDeCompraRepository>();
            _fornecedoresRepository = Substitute.For<IFornecedorRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.PedidosDeCompraRepository.Returns(_pedidosRepository);
            _unitOfWork.FornecedoresRepository.Returns(_fornecedoresRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new CriarPedidoDeCompraUseCase(_unitOfWork, _logService);
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoItemPossuirQuantidadeInvalida()
        {
            var entrada = new CriarPedidoDeCompraEntrada(
                IdFornecedor: 202604020002,
                Itens: new List<ItemPedidoDeCompraEntrada>
                {
                    new(202604020001, 0m, 5.00m)
                });

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("QUANTIDADE_DEVE_SER_POSITIVA");

            await _fornecedoresRepository
                .DidNotReceive()
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>());

            await _pedidosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoFornecedorNaoExistir()
        {
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("FORNECEDOR_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoExistir()
        {
            var entrada = CriarEntradaValida();

            _fornecedoresRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _produtosRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_ENCONTRADO");

            await _pedidosRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoSaveChangesFalhar()
        {
            var entrada = CriarEntradaValida();
            ConfigurarExistencias();

            _pedidosRepository
                .AdicionarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Falha("ERRO_AO_SALVAR"));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ERRO_AO_SALVAR");
        }

        [Fact]
        public async Task ExecutarAsync_DeveCriarPedidoComSucesso_QuandoDadosForemValidos()
        {
            var entrada = CriarEntradaValida();
            ConfigurarExistencias();

            _pedidosRepository
                .AdicionarAsync(Arg.Any<PedidoDeCompra>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(entrada);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("EmEdicao");
            resultado.Instancia.ValorTotal.Should().Be(50.00m);
            resultado.Instancia.Itens.Should().ContainSingle();

            await _pedidosRepository
                .Received(1)
                .AdicionarAsync(
                    Arg.Is<PedidoDeCompra>(p =>
                        p.IdFornecedor.Valor == entrada.IdFornecedor &&
                        p.EstaEmEdicao &&
                        p.Itens.Count == 1),
                    Arg.Any<CancellationToken>());

            await _unitOfWork
                .Received(1)
                .SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        private void ConfigurarExistencias()
        {
            _fornecedoresRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));

            _produtosRepository
                .ExistePorIdAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
        }

        private static CriarPedidoDeCompraEntrada CriarEntradaValida()
        {
            return new CriarPedidoDeCompraEntrada(
                IdFornecedor: 202604020002,
                Itens: new List<ItemPedidoDeCompraEntrada>
                {
                    new(202604020001, 10m, 5.00m)
                });
        }
    }
}
