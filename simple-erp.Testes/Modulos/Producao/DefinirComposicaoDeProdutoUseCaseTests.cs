using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Core.Modulos.Producao.Composicao.UseCases;
using simple_erp.Testes.Compartilhado.Builders;
using System.Collections.Generic;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class DefinirComposicaoDeProdutoUseCaseTests
    {
        private const long IdFabricado = 202604020001;
        private const long IdInsumo = 202604020010;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IComposicaoDeProdutoRepository _composicoesRepository;
        private readonly IProdutoRepository _produtosRepository;
        private readonly ILogService _logService;
        private readonly DefinirComposicaoDeProdutoUseCase _useCase;

        public DefinirComposicaoDeProdutoUseCaseTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _composicoesRepository = Substitute.For<IComposicaoDeProdutoRepository>();
            _produtosRepository = Substitute.For<IProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ComposicoesDeProdutoRepository.Returns(_composicoesRepository);
            _unitOfWork.ProdutosRepository.Returns(_produtosRepository);

            _useCase = new DefinirComposicaoDeProdutoUseCase(_unitOfWork, _logService);
        }

        private static DefinirComposicaoDeProdutoEntrada EntradaValida() =>
            new(IdFabricado, new List<ItemDeComposicaoEntrada> { new(IdInsumo, 2m) });

        private void RetornarProduto(long id, Produto produto) =>
            _produtosRepository
                .ObterPorIdAsync(Arg.Is<Id>(i => i.Valor == id), Arg.Any<CancellationToken>())
                .Returns(Resultado<Produto>.Sucesso(produto));

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoProdutoNaoForFabricado()
        {
            RetornarProduto(IdFabricado, ProdutoBuilder.Novo().ComId(IdFabricado).SemClassificacao().Criar());

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PRODUTO_NAO_E_FABRICADO");

            await _composicoesRepository
                .DidNotReceive()
                .AdicionarAsync(Arg.Any<ComposicaoDeProduto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecutarAsync_DeveRetornarFalha_QuandoInsumoNaoForMateriaPrima()
        {
            RetornarProduto(IdFabricado, ProdutoBuilder.Novo().ComId(IdFabricado).ComoFabricado().Criar());
            RetornarProduto(IdInsumo, ProdutoBuilder.Novo().ComId(IdInsumo).ComCodigo("MP-1").SemClassificacao().Criar());

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("INSUMO_NAO_E_MATERIA_PRIMA");
        }

        [Fact]
        public async Task ExecutarAsync_DeveDefinirComposicaoInativa_QuandoDadosForemValidos()
        {
            RetornarProduto(IdFabricado, ProdutoBuilder.Novo().ComId(IdFabricado).ComoFabricado().Criar());
            RetornarProduto(IdInsumo, ProdutoBuilder.Novo().ComId(IdInsumo).ComCodigo("MP-1").ComoMateriaPrima().Criar());

            _composicoesRepository
                .ObterProximaVersaoPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));
            _composicoesRepository
                .AdicionarAsync(Arg.Any<ComposicaoDeProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _useCase.ExecutarAsync(EntradaValida());

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.Versao.Should().Be(1);
            resultado.Instancia.Ativa.Should().BeFalse();
            resultado.Instancia.Itens.Should().ContainSingle(i => i.IdInsumo == IdInsumo);

            await _composicoesRepository
                .Received(1)
                .AdicionarAsync(Arg.Is<ComposicaoDeProduto>(c => c.Versao == 1 && !c.Ativa), Arg.Any<CancellationToken>());
        }
    }
}
