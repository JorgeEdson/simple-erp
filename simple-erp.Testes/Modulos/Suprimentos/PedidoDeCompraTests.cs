using FluentAssertions;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using simple_erp.Testes.Compartilhado.Builders;
using System.Linq;

namespace simple_erp.Testes.Modulos.Suprimentos
{
    public sealed class PedidoDeCompraTests
    {
        private static ItemDePedidoDeCompra CriarItem(long idProduto, decimal quantidade, decimal custo)
        {
            var item = ItemDePedidoDeCompra.TentarCriar(
                Id.TentarCriar(idProduto).Instancia,
                Quantidade.TentarCriar(quantidade).Instancia,
                Dinheiro.TentarCriar(custo).Instancia);

            return item.Instancia;
        }

        [Fact]
        public void Criar_DeveIniciarEmEdicaoEEmitirEvento_QuandoFornecedorValido()
        {
            var idFornecedor = Id.TentarCriar(202604020002).Instancia;

            var resultado = PedidoDeCompra.Criar(idFornecedor);

            resultado.EhSucesso.Should().BeTrue();
            resultado.Instancia.EstaEmEdicao.Should().BeTrue();
            resultado.Instancia.Itens.Should().BeEmpty();
            resultado.Instancia.EventosDeDominio
                .OfType<PedidoDeCompraCriado>()
                .Should().ContainSingle();
        }

        [Fact]
        public void Criar_DeveFalhar_QuandoHouverProdutoDuplicadoNosItensIniciais()
        {
            var idFornecedor = Id.TentarCriar(202604020002).Instancia;

            var itens = new[]
            {
                CriarItem(202604020001, 5m, 2.00m),
                CriarItem(202604020001, 3m, 4.00m)
            };

            var resultado = PedidoDeCompra.Criar(idFornecedor, itens);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ITEM_PRODUTO_DUPLICADO");
        }

        [Fact]
        public void ValorTotal_DeveSomarSubtotaisDosItens()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)   // 50,00
                .ComItem(202604020010, 3m, 2.50m)     // 7,50
                .Criar();

            pedido.ValorTotal.Valor.Should().Be(57.50m);
        }

        [Fact]
        public void AdicionarItem_DeveFalhar_QuandoProdutoJaEstiverNoPedido()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Criar();

            var itemDuplicado = CriarItem(202604020001, 1m, 1.00m);

            var resultado = pedido.AdicionarItem(itemDuplicado);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ITEM_PRODUTO_DUPLICADO");
            pedido.Itens.Should().HaveCount(1);
        }

        [Fact]
        public void AdicionarItem_DeveFalhar_QuandoPedidoNaoEstiverEmEdicao()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Aprovado().Criar();

            var item = CriarItem(202604020099, 1m, 1.00m);

            var resultado = pedido.AdicionarItem(item);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_EDITAVEL");
        }

        [Fact]
        public void RemoverItem_DeveFalhar_QuandoItemNaoExistir()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Criar();

            var resultado = pedido.RemoverItem(999);

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("ITEM_NAO_ENCONTRADO");
        }

        [Fact]
        public void Aprovar_DeveFalhar_QuandoNaoHouverItens()
        {
            var idFornecedor = Id.TentarCriar(202604020002).Instancia;
            var pedido = PedidoDeCompra.Criar(idFornecedor).Instancia;

            var resultado = pedido.Aprovar();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_SEM_ITENS");
        }

        [Fact]
        public void Aprovar_DeveTransicionarParaAprovadaEEmitirEvento_QuandoHouverItens()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar();

            var resultado = pedido.Aprovar();

            resultado.EhSucesso.Should().BeTrue();
            pedido.EstaAprovado.Should().BeTrue();
            pedido.EventosDeDominio
                .OfType<PedidoDeCompraAprovado>()
                .Should().ContainSingle();
        }

        [Fact]
        public void Efetivar_DeveFalhar_QuandoPedidoNaoEstiverAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar(); // em edição

            var resultado = pedido.Efetivar();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_NAO_APROVADO_NAO_PODE_SER_EFETIVADO");
        }

        [Fact]
        public void Efetivar_DeveConcluirEEmitirEventoComItens_QuandoAprovado()
        {
            var pedido = PedidoDeCompraBuilder.Novo()
                .SemItens()
                .ComItem(202604020001, 10m, 5.00m)
                .Aprovado()
                .Criar();

            var resultado = pedido.Efetivar();

            resultado.EhSucesso.Should().BeTrue();
            pedido.EstaConcluido.Should().BeTrue();

            var evento = pedido.EventosDeDominio
                .OfType<PedidoDeCompraEfetivado>()
                .Should().ContainSingle().Subject;

            evento.ValorTotal.Should().Be(50.00m);
            evento.Itens.Should().ContainSingle(i => i.IdProduto == 202604020001 && i.Quantidade == 10m);
        }

        [Fact]
        public void Cancelar_DeveFalhar_QuandoPedidoJaEstiverConcluido()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Concluido().Criar();

            var resultado = pedido.Cancelar();

            resultado.EhFalha.Should().BeTrue();
            resultado.Erros.Should().Contain("PEDIDO_DE_COMPRA_CONCLUIDO_NAO_PODE_SER_CANCELADO");
        }

        [Fact]
        public void Cancelar_DeveTransicionarParaCancelada_QuandoEmEdicao()
        {
            var pedido = PedidoDeCompraBuilder.Novo().Criar();

            var resultado = pedido.Cancelar();

            resultado.EhSucesso.Should().BeTrue();
            pedido.EstaCancelado.Should().BeTrue();
            pedido.EventosDeDominio
                .OfType<PedidoDeCompraCancelado>()
                .Should().ContainSingle();
        }
    }
}
