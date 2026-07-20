using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Vendas.Eventos;
using simple_erp.Core.Modulos.Vendas.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Core.Modulos.Vendas.Entidades
{
  
    public sealed class PedidoDeVenda : Entidade<PedidoDeVenda>
    {
        private readonly List<ItemDePedidoDeVenda> _itens;

        private PedidoDeVenda(
            int numero,
            Id idCliente,
            StatusPedidoDeVenda status,
            decimal descontoDoPedido,
            string? motivoCancelamento,
            IEnumerable<ItemDePedidoDeVenda> itens,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            Numero = numero;
            IdCliente = idCliente;
            Status = status;
            DescontoDoPedido = descontoDoPedido;
            MotivoCancelamento = motivoCancelamento;
            _itens = itens.ToList();
        }

        public int Numero { get; private set; }
        public Id IdCliente { get; private set; }
        public StatusPedidoDeVenda Status { get; private set; }
        public decimal DescontoDoPedido { get; private set; }
        public string? MotivoCancelamento { get; private set; }

        public IReadOnlyCollection<ItemDePedidoDeVenda> Itens => _itens.AsReadOnly();

        public bool EstaEmEdicao => Status == StatusPedidoDeVenda.EmEdicao;
        public bool EstaAprovado => Status == StatusPedidoDeVenda.Aprovado;
        public bool EstaCancelado => Status == StatusPedidoDeVenda.Cancelado;
        public bool EstaConcluido => Status == StatusPedidoDeVenda.Concluido;
        public bool PossuiItens => _itens.Count > 0;

        public decimal ValorBrutoItens => _itens.Sum(item => item.ValorBruto);
        public decimal ValorDescontoItens => _itens.Sum(item => item.Desconto);
        public decimal ValorSubtotalItens => _itens.Sum(item => item.Subtotal);

        public Dinheiro ValorTotal
        {
            get
            {
                var total = ValorSubtotalItens - DescontoDoPedido;
                return Dinheiro.TentarCriar(total < 0m ? 0m : total).Instancia;
            }
        }

        public static Resultado<PedidoDeVenda> Criar(
            int numero,
            Id idCliente,
            IEnumerable<ItemDePedidoDeVenda>? itens = null,
            Dinheiro? descontoDoPedido = null,
            long? id = null)
        {
            if (numero <= 0)
                return Resultado<PedidoDeVenda>.Falha("NUMERO_PEDIDO_INVALIDO");

            if (idCliente is null)
                return Resultado<PedidoDeVenda>.Falha("CLIENTE_OBRIGATORIO");

            var listaItens = itens?.ToList() ?? new List<ItemDePedidoDeVenda>();

            if (listaItens.Any(item => item is null))
                return Resultado<PedidoDeVenda>.Falha("ITEM_OBRIGATORIO");

            var possuiDuplicados = listaItens
                .GroupBy(item => item.IdProduto)
                .Any(grupo => grupo.Count() > 1);

            if (possuiDuplicados)
                return Resultado<PedidoDeVenda>.Falha("ITEM_PRODUTO_DUPLICADO");

            var desconto = descontoDoPedido?.Valor ?? 0m;
            var subtotal = listaItens.Sum(item => item.Subtotal);

            if (desconto > subtotal)
                return Resultado<PedidoDeVenda>.Falha("DESCONTO_PEDIDO_INVALIDO");

            var pedido = new PedidoDeVenda(
                numero,
                idCliente,
                StatusPedidoDeVenda.EmEdicao,
                desconto,
                motivoCancelamento: null,
                listaItens,
                id);

            pedido.AdicionarEventoDeDominio(
                new PedidoDeVendaCriado(pedido.Id, pedido.IdCliente, pedido.Numero));

            return Resultado<PedidoDeVenda>.Sucesso(pedido);
        }

        public Resultado<bool> AdicionarItem(ItemDePedidoDeVenda item)
        {
            if (item is null)
                return Resultado<bool>.Falha("ITEM_OBRIGATORIO");

            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_NAO_EDITAVEL");

            if (_itens.Any(existente => existente.IdProduto == item.IdProduto))
                return Resultado<bool>.Falha("ITEM_PRODUTO_DUPLICADO");

            _itens.Add(item);
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> RemoverItem(long idProduto)
        {
            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_NAO_EDITAVEL");

            var item = _itens.FirstOrDefault(existente => existente.IdProduto == idProduto);

            if (item is null)
                return Resultado<bool>.Falha("ITEM_NAO_ENCONTRADO");

            _itens.Remove(item);

            if (DescontoDoPedido > ValorSubtotalItens)
                DescontoDoPedido = ValorSubtotalItens;

            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AplicarDescontoNoPedido(Dinheiro desconto)
        {
            if (desconto is null)
                return Resultado<bool>.Falha("DESCONTO_OBRIGATORIO");

            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_NAO_EDITAVEL");

            if (desconto.Valor > ValorSubtotalItens)
                return Resultado<bool>.Falha("DESCONTO_PEDIDO_INVALIDO");

            DescontoDoPedido = desconto.Valor;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        /// <summary>
        /// Aprova o pedido, congelando valores e condições (a edição passa a ser
        /// bloqueada). A validação de disponibilidade de estoque e a baixa (saída por
        /// venda) são orquestradas pelo caso de uso; o agregado emite o evento com os
        /// itens para essa geração.
        /// </summary>
        public Resultado<bool> Aprovar()
        {
            if (EstaAprovado)
                return Resultado<bool>.Sucesso(true);

            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_NAO_PODE_SER_APROVADO");

            if (!PossuiItens)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_SEM_ITENS");

            Status = StatusPedidoDeVenda.Aprovado;
            AtualizarDataAtualizacao();

            var itensEvento = _itens
                .Select(item => new ItemVendaAprovado(item.IdProduto, item.Quantidade))
                .ToList();

            AdicionarEventoDeDominio(new PedidoDeVendaAprovado(
                Id,
                IdCliente,
                ValorTotal.Valor,
                itensEvento));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Concluir()
        {
            if (EstaConcluido)
                return Resultado<bool>.Sucesso(true);

            if (!EstaAprovado)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_NAO_APROVADO_NAO_PODE_SER_CONCLUIDO");

            Status = StatusPedidoDeVenda.Concluido;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new PedidoDeVendaConcluido(Id));

            return Resultado<bool>.Sucesso(true);
        }

        /// <summary>
        /// Cancela o pedido com motivo. Não cancela um pedido concluído. Observação:
        /// o estorno de estoque de um pedido já aprovado (baixa realizada) não faz
        /// parte do escopo dos requisitos e exigiria um movimento compensatório.
        /// </summary>
        public Resultado<bool> Cancelar(MotivoCancelamento motivo)
        {
            if (motivo is null)
                return Resultado<bool>.Falha("MOTIVO_CANCELAMENTO_OBRIGATORIO");

            if (EstaCancelado)
                return Resultado<bool>.Sucesso(true);

            if (EstaConcluido)
                return Resultado<bool>.Falha("PEDIDO_DE_VENDA_CONCLUIDO_NAO_PODE_SER_CANCELADO");

            Status = StatusPedidoDeVenda.Cancelado;
            MotivoCancelamento = motivo.Valor;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new PedidoDeVendaCancelado(Id, motivo.Valor));

            return Resultado<bool>.Sucesso(true);
        }

        public static PedidoDeVenda Reconstituir(
            int numero,
            Id idCliente,
            StatusPedidoDeVenda status,
            decimal descontoDoPedido,
            string? motivoCancelamento,
            IEnumerable<ItemDePedidoDeVenda> itens,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new PedidoDeVenda(
                numero,
                idCliente,
                status,
                descontoDoPedido,
                motivoCancelamento,
                itens,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
