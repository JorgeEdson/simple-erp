using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Entidades;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class PedidoDeCompraBuilder
    {
        private long? _id = 202604020003;
        private long _idFornecedor = 202604020002;

        private readonly List<(long IdProduto, decimal Quantidade, decimal CustoUnitario)> _itens = new()
        {
            (202604020001, 10m, 5.00m)
        };

        private StatusPedidoDeCompra _statusAlvo = StatusPedidoDeCompra.EmEdicao;

        public static PedidoDeCompraBuilder Novo() => new();

        public PedidoDeCompraBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public PedidoDeCompraBuilder SemId()
        {
            _id = null;
            return this;
        }

        public PedidoDeCompraBuilder ComIdFornecedor(long idFornecedor)
        {
            _idFornecedor = idFornecedor;
            return this;
        }

        public PedidoDeCompraBuilder SemItens()
        {
            _itens.Clear();
            return this;
        }

        public PedidoDeCompraBuilder ComItem(long idProduto, decimal quantidade, decimal custoUnitario)
        {
            _itens.Add((idProduto, quantidade, custoUnitario));
            return this;
        }

        public PedidoDeCompraBuilder EmEdicao()
        {
            _statusAlvo = StatusPedidoDeCompra.EmEdicao;
            return this;
        }

        public PedidoDeCompraBuilder Aprovado()
        {
            _statusAlvo = StatusPedidoDeCompra.Aprovada;
            return this;
        }

        public PedidoDeCompraBuilder Cancelado()
        {
            _statusAlvo = StatusPedidoDeCompra.Cancelada;
            return this;
        }

        public PedidoDeCompraBuilder Concluido()
        {
            _statusAlvo = StatusPedidoDeCompra.Concluida;
            return this;
        }

        public PedidoDeCompra Criar()
        {
            var resultadoIdFornecedor = Id.TentarCriar(_idFornecedor);

            if (resultadoIdFornecedor.EhFalha)
                throw new InvalidOperationException(
                    $"Id de fornecedor inválido no builder: {string.Join(", ", resultadoIdFornecedor.Erros!)}");

            var itens = _itens
                .Select(item => CriarItem(item.IdProduto, item.Quantidade, item.CustoUnitario))
                .ToList();

            var resultadoPedido = PedidoDeCompra.Criar(
                resultadoIdFornecedor.Instancia,
                itens,
                _id);

            if (resultadoPedido.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar PedidoDeCompra válido para o teste. Erros: {string.Join(", ", resultadoPedido.Erros!)}");

            var pedido = resultadoPedido.Instancia;

            AplicarStatusAlvo(pedido);

            return pedido;
        }

        private static ItemDePedidoDeCompra CriarItem(long idProduto, decimal quantidade, decimal custoUnitario)
        {
            var resultadoIdProduto = Id.TentarCriar(idProduto);
            var resultadoQuantidade = Quantidade.TentarCriar(quantidade);
            var resultadoCusto = Dinheiro.TentarCriar(custoUnitario);

            if (resultadoIdProduto.EhFalha)
                throw new InvalidOperationException($"Id de produto inválido no builder: {string.Join(", ", resultadoIdProduto.Erros!)}");

            if (resultadoQuantidade.EhFalha)
                throw new InvalidOperationException($"Quantidade inválida no builder: {string.Join(", ", resultadoQuantidade.Erros!)}");

            if (resultadoCusto.EhFalha)
                throw new InvalidOperationException($"Custo unitário inválido no builder: {string.Join(", ", resultadoCusto.Erros!)}");

            var resultadoItem = ItemDePedidoDeCompra.TentarCriar(
                resultadoIdProduto.Instancia,
                resultadoQuantidade.Instancia,
                resultadoCusto.Instancia);

            if (resultadoItem.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar ItemDePedidoDeCompra válido para o teste. Erros: {string.Join(", ", resultadoItem.Erros!)}");

            return resultadoItem.Instancia;
        }

        private void AplicarStatusAlvo(PedidoDeCompra pedido)
        {
            switch (_statusAlvo)
            {
                case StatusPedidoDeCompra.EmEdicao:
                    break;

                case StatusPedidoDeCompra.Aprovada:
                    GarantirSucesso(pedido.Aprovar(), nameof(pedido.Aprovar));
                    break;

                case StatusPedidoDeCompra.Cancelada:
                    GarantirSucesso(pedido.Cancelar(), nameof(pedido.Cancelar));
                    break;

                case StatusPedidoDeCompra.Concluida:
                    GarantirSucesso(pedido.Aprovar(), nameof(pedido.Aprovar));
                    GarantirSucesso(pedido.Efetivar(), nameof(pedido.Efetivar));
                    break;
            }

            pedido.LimparEventosDeDominio();
        }

        private static void GarantirSucesso(
            simple_erp.Core.Compartilhado.Base.Resultado<bool> resultado,
            string operacao)
        {
            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Falha ao aplicar '{operacao}' no builder de PedidoDeCompra: {string.Join(", ", resultado.Erros!)}");
        }
    }
}
