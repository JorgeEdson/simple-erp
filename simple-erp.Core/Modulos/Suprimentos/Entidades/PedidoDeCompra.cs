using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Suprimentos.Eventos;
using simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simple_erp.Core.Modulos.Suprimentos.Entidades
{
    public sealed class PedidoDeCompra : Entidade<PedidoDeCompra>
    {
        // Não é readonly para que o provider de persistência (EF Core) possa
        // materializar a coleção a partir da coluna jsonb pelo campo de apoio.
        private List<ItemDePedidoDeCompra> _itens;

#pragma warning disable CS8618 // Construtor de materialização do EF Core: as propriedades são preenchidas pelo provider.
        /// <summary>Construtor de materialização do EF Core.</summary>
        private PedidoDeCompra()
        {
            _itens = new List<ItemDePedidoDeCompra>();
        }
#pragma warning restore CS8618

        private PedidoDeCompra(
            Id idFornecedor,
            StatusPedidoDeCompra status,
            IEnumerable<ItemDePedidoDeCompra>? itens = null,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            IdFornecedor = idFornecedor;
            Status = status;
            _itens = itens?.ToList() ?? new List<ItemDePedidoDeCompra>();
        }

        public Id IdFornecedor { get; private set; }
        public StatusPedidoDeCompra Status { get; private set; }

        public IReadOnlyCollection<ItemDePedidoDeCompra> Itens => _itens.AsReadOnly();

        public bool EstaEmEdicao => Status == StatusPedidoDeCompra.EmEdicao;
        public bool EstaAprovado => Status == StatusPedidoDeCompra.Aprovada;
        public bool EstaCancelado => Status == StatusPedidoDeCompra.Cancelada;
        public bool EstaConcluido => Status == StatusPedidoDeCompra.Concluida;

        public bool PossuiItens => _itens.Count > 0;
        
        public Dinheiro ValorTotal
        {
            get
            {
                var total = _itens.Sum(item => item.Subtotal);
                return Dinheiro.TentarCriar(total).Instancia;
            }
        }

        public static Resultado<PedidoDeCompra> Criar(
            Id idFornecedor,
            IEnumerable<ItemDePedidoDeCompra>? itens = null,
            long? id = null)
        {
            if (idFornecedor is null)
                return Resultado<PedidoDeCompra>.Falha("FORNECEDOR_OBRIGATORIO");

            var listaItens = itens?.ToList() ?? new List<ItemDePedidoDeCompra>();

            if (listaItens.Any(item => item is null))
                return Resultado<PedidoDeCompra>.Falha("ITEM_OBRIGATORIO");

            var possuiDuplicados = listaItens
                .GroupBy(item => item.IdProduto)
                .Any(grupo => grupo.Count() > 1);

            if (possuiDuplicados)
                return Resultado<PedidoDeCompra>.Falha("ITEM_PRODUTO_DUPLICADO");

            var pedido = new PedidoDeCompra(
                idFornecedor,
                StatusPedidoDeCompra.EmEdicao,
                listaItens,
                id);

            pedido.AdicionarEventoDeDominio(
                new PedidoDeCompraCriado(pedido.Id, pedido.IdFornecedor));

            return Resultado<PedidoDeCompra>.Sucesso(pedido);
        }

        public Resultado<bool> AdicionarItem(ItemDePedidoDeCompra item)
        {
            if (item is null)
                return Resultado<bool>.Falha("ITEM_OBRIGATORIO");

            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_NAO_EDITAVEL");

            if (_itens.Any(existente => existente.IdProduto == item.IdProduto))
                return Resultado<bool>.Falha("ITEM_PRODUTO_DUPLICADO");

            _itens.Add(item);
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> RemoverItem(long idProduto)
        {
            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_NAO_EDITAVEL");

            var item = _itens.FirstOrDefault(existente => existente.IdProduto == idProduto);

            if (item is null)
                return Resultado<bool>.Falha("ITEM_NAO_ENCONTRADO");

            _itens.Remove(item);
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Aprovar()
        {
            if (EstaAprovado)
                return Resultado<bool>.Sucesso(true);

            if (!EstaEmEdicao)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_NAO_PODE_SER_APROVADO");

            if (!PossuiItens)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_SEM_ITENS");

            Status = StatusPedidoDeCompra.Aprovada;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(
                new PedidoDeCompraAprovado(Id, IdFornecedor, ValorTotal.Valor));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Cancelar()
        {
            if (EstaCancelado)
                return Resultado<bool>.Sucesso(true);

            if (EstaConcluido)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_CONCLUIDO_NAO_PODE_SER_CANCELADO");

            Status = StatusPedidoDeCompra.Cancelada;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new PedidoDeCompraCancelado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        
        public Resultado<bool> Efetivar()
        {
            if (EstaConcluido)
                return Resultado<bool>.Sucesso(true);

            if (!EstaAprovado)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_NAO_APROVADO_NAO_PODE_SER_EFETIVADO");

            if (!PossuiItens)
                return Resultado<bool>.Falha("PEDIDO_DE_COMPRA_SEM_ITENS");

            Status = StatusPedidoDeCompra.Concluida;
            AtualizarDataAtualizacao();

            var itensEvento = _itens
                .Select(item => new ItemPedidoDeCompraEfetivado(
                    item.IdProduto,
                    item.Quantidade,
                    item.CustoUnitario))
                .ToList();

            AdicionarEventoDeDominio(
                new PedidoDeCompraEfetivado(
                    Id,
                    IdFornecedor,
                    ValorTotal.Valor,
                    itensEvento));

            return Resultado<bool>.Sucesso(true);
        }

        public static PedidoDeCompra Reconstituir(
            Id idFornecedor,
            StatusPedidoDeCompra status,
            IEnumerable<ItemDePedidoDeCompra> itens,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new PedidoDeCompra(
                idFornecedor,
                status,
                itens,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
