using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Entidades;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class PedidoDeVendaBuilder
    {
        private long? _id = 202604020500;
        private int _numero = 1;
        private long _idCliente = 202604020002;
        private decimal _descontoDoPedido = 0m;

        // (IdProduto, Quantidade, PrecoUnitario, Desconto)
        private readonly List<(long, decimal, decimal, decimal)> _itens = new()
        {
            (202604020001, 2m, 10.00m, 0m)
        };

        private StatusPedidoDeVenda _statusAlvo = StatusPedidoDeVenda.EmEdicao;
        private string _motivoCancelamento = "Cancelamento de teste";

        public static PedidoDeVendaBuilder Novo() => new();

        public PedidoDeVendaBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public PedidoDeVendaBuilder ComNumero(int numero)
        {
            _numero = numero;
            return this;
        }

        public PedidoDeVendaBuilder ComIdCliente(long idCliente)
        {
            _idCliente = idCliente;
            return this;
        }

        public PedidoDeVendaBuilder ComDescontoDoPedido(decimal desconto)
        {
            _descontoDoPedido = desconto;
            return this;
        }

        public PedidoDeVendaBuilder SemItens()
        {
            _itens.Clear();
            return this;
        }

        public PedidoDeVendaBuilder ComItem(long idProduto, decimal quantidade, decimal precoUnitario, decimal desconto = 0m)
        {
            _itens.Add((idProduto, quantidade, precoUnitario, desconto));
            return this;
        }

        public PedidoDeVendaBuilder EmEdicao()
        {
            _statusAlvo = StatusPedidoDeVenda.EmEdicao;
            return this;
        }

        public PedidoDeVendaBuilder Aprovado()
        {
            _statusAlvo = StatusPedidoDeVenda.Aprovado;
            return this;
        }

        public PedidoDeVendaBuilder Concluido()
        {
            _statusAlvo = StatusPedidoDeVenda.Concluido;
            return this;
        }

        public PedidoDeVendaBuilder Cancelado()
        {
            _statusAlvo = StatusPedidoDeVenda.Cancelado;
            return this;
        }

        public PedidoDeVenda Criar()
        {
            var idCliente = Id.TentarCriar(_idCliente).Instancia;

            var itens = _itens
                .Select(item => ItemDePedidoDeVenda.TentarCriar(
                    Id.TentarCriar(item.Item1).Instancia,
                    Quantidade.TentarCriar(item.Item2).Instancia,
                    Dinheiro.TentarCriar(item.Item3).Instancia,
                    Dinheiro.TentarCriar(item.Item4).Instancia).Instancia)
                .ToList();

            var desconto = Dinheiro.TentarCriar(_descontoDoPedido).Instancia;

            var resultado = PedidoDeVenda.Criar(_numero, idCliente, itens, desconto, _id);

            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar PedidoDeVenda válido para o teste. Erros: {string.Join(", ", resultado.Erros!)}");

            var pedido = resultado.Instancia;

            switch (_statusAlvo)
            {
                case StatusPedidoDeVenda.EmEdicao:
                    break;
                case StatusPedidoDeVenda.Aprovado:
                    pedido.Aprovar();
                    break;
                case StatusPedidoDeVenda.Concluido:
                    pedido.Aprovar();
                    pedido.Concluir();
                    break;
                case StatusPedidoDeVenda.Cancelado:
                    pedido.Cancelar(MotivoCancelamento.TentarCriar(_motivoCancelamento).Instancia);
                    break;
            }

            pedido.LimparEventosDeDominio();

            return pedido;
        }
    }
}
