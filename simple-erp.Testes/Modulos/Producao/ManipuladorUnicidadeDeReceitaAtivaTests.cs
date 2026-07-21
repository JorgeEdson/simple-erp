using FluentAssertions;
using NSubstitute;
using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.Eventos;
using simple_erp.Core.Modulos.Producao.Composicao.Handlers;
using simple_erp.Core.Modulos.Producao.Composicao.Interfaces.Repositorios;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.Producao
{
    public sealed class ManipuladorUnicidadeDeReceitaAtivaTests
    {
        private const long IdComposicaoNova = 202604020301;
        private const long IdComposicaoAntiga = 202604020300;
        private const long IdProdutoFabricado = 202604020050;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IComposicaoDeProdutoRepository _composicoesRepository;
        private readonly ILogService _logService;
        private readonly ManipuladorUnicidadeDeReceitaAtiva _handler;

        public ManipuladorUnicidadeDeReceitaAtivaTests()
        {
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _composicoesRepository = Substitute.For<IComposicaoDeProdutoRepository>();
            _logService = Substitute.For<ILogService>();

            _unitOfWork.ComposicoesDeProdutoRepository.Returns(_composicoesRepository);

            _handler = new ManipuladorUnicidadeDeReceitaAtiva(_unitOfWork, _logService);
        }

        private static ComposicaoDeProdutoAtivada Evento(int versao = 2) =>
            new(
                Id.TentarCriar(IdComposicaoNova).Instancia,
                Id.TentarCriar(IdProdutoFabricado).Instancia,
                versao);

        [Fact]
        public async Task ManipularAsync_DeveDesativarVersaoAnterior_QuandoOutraVersaoEstiverAtiva()
        {
            var versaoAntiga = ComposicaoDeProdutoBuilder.Novo()
                .ComId(IdComposicaoAntiga)
                .ComIdProdutoFabricado(IdProdutoFabricado)
                .ComVersao(1)
                .Ativa()
                .Criar();

            _composicoesRepository
                .ExisteAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _composicoesRepository
                .ObterAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<ComposicaoDeProduto?>.Sucesso(versaoAntiga));
            _composicoesRepository
                .AtualizarAsync(Arg.Any<ComposicaoDeProduto>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .Returns(Resultado<int>.Sucesso(1));

            var resultado = await _handler.ManipularAsync(Evento());

            resultado.EhSucesso.Should().BeTrue();
            versaoAntiga.Ativa.Should().BeFalse();

            await _composicoesRepository
                .Received(1)
                .AtualizarAsync(
                    Arg.Is<ComposicaoDeProduto>(c => c.Id.Valor == IdComposicaoAntiga && !c.Ativa),
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_NaoDeveFazerNada_QuandoNaoHouverOutraVersaoAtiva()
        {
            _composicoesRepository
                .ExisteAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(false));

            var resultado = await _handler.ManipularAsync(Evento());

            resultado.EhSucesso.Should().BeTrue();

            await _composicoesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<ComposicaoDeProduto>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ManipularAsync_NaoDeveFazerNada_QuandoAtivaForAPropriaComposicaoDoEvento()
        {
            var propria = ComposicaoDeProdutoBuilder.Novo()
                .ComId(IdComposicaoNova)
                .ComIdProdutoFabricado(IdProdutoFabricado)
                .ComVersao(2)
                .Ativa()
                .Criar();

            _composicoesRepository
                .ExisteAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<bool>.Sucesso(true));
            _composicoesRepository
                .ObterAtivaPorProdutoAsync(Arg.Any<Id>(), Arg.Any<CancellationToken>())
                .Returns(Resultado<ComposicaoDeProduto?>.Sucesso(propria));

            var resultado = await _handler.ManipularAsync(Evento());

            resultado.EhSucesso.Should().BeTrue();
            propria.Ativa.Should().BeTrue();

            await _composicoesRepository
                .DidNotReceive()
                .AtualizarAsync(Arg.Any<ComposicaoDeProduto>(), Arg.Any<CancellationToken>());
        }
    }
}
