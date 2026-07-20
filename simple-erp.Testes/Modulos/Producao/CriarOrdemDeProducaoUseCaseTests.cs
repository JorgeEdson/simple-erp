using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.UseCases;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class CriarOrdemDeProducaoUseCaseTests
    {
        private const long IdFabricado = 202604020001;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrdemDeProducaoRepository _ordensRepository;
        private readonly IComposicaoDeProdutoRepository _composicoesRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly CriarOrdemDeProducaoUseCase _useCase;

        public CriarOrdemDeProducaoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _ordensRepository = Substitute.For<IOrdemDeProducaoRepository>();
            _composicoesRepository = Substitute.For<IComposicaoDeProdutoRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.OrdensDeProducaoRepository.Returns(_ordensRepository);
            _unitOfWork.ComposicoesDeProdutoRepository.Returns(_composicoesRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new CriarOrdemDeProducaoUseCase(_unitOfWork, _logService);
        }

        private void RetornarProdutoFabricado() =>
            _produtosRepository
                .ObterPorIdAsync(Arg.Is<Id>(i => i.Valor == IdFabricado), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Sucesso(
                    ProdutoBuilder.Novo().ComId(IdFabricado).ComoFabricado().Criar()));

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoNaoHouverComposicaoAtiva()
        {
            RetornarProdutoFabricado();

            _composicoesRepository
                .ExisteAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _useCase.ExecutarAsync(new CriarOrdemDeProducaoEntrada(IdFabricado, 5m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("COMPOSICAO_ATIVA_NAO_ENCONTRADA");

            await _ordensRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoForFabricado()
        {
            _produtosRepository
                .ObterPorIdAsync(Arg.Is<Id>(i => i.Valor == IdFabricado), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Sucesso(
                    ProdutoBuilder.Novo().ComId(IdFabricado).SemClassificacao().Criar()));

            var resultado = await _useCase.ExecutarAsync(new CriarOrdemDeProducaoEntrada(IdFabricado, 5m));

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_E_FABRICADO");
        }

        [Fact]
        public async Task ExecutarAsync_DeveCalcularNecessidadesECriarOrdem_QuandoDadosForemValidos()
        {
            RetornarProdutoFabricado();

            var composicao = ComposicaoDeProdutoBuilder.Novo()
                .ComIdProdutoFabricado(IdFabricado)
                .SemItens()
                .ComItem(202604020010, 2m)
                .ComItem(202604020011, 3m)
                .Ativa()
                .Criar();

            _composicoesRepository
                .ExisteAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _composicoesRepository
                .ObterAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<ComposicaoDeProduto?>.Sucesso(composicao));
            _ordensRepository
                .AdicionarAsync(Arg.Any<OrdemDeProducao>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(new CriarOrdemDeProducaoEntrada(IdFabricado, 5m));

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Status.Should().Be("Criada");
            resultado.Instancia.Necessidades.Should().ContainSingle(n => n.IdInsumo == 202604020010 && n.QuantidadeNecessaria == 10m);
            resultado.Instancia.Necessidades.Should().ContainSingle(n => n.IdInsumo == 202604020011 && n.QuantidadeNecessaria == 15m);

            await _ordensRepository
                .Received(1)
                .AdicionarAsync(Arg.Is<OrdemDeProducao>(o => o.EstaCriada && o.Necessidades.Count == 2), Arg.Any<CancellationToken>());
        }
    }
}
