using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Collections.Generic;

namespace simple_erp.Core.Modulos.Vendas.ObjetosDeValor
{
    /// <summary>
    /// Item de um pedido de venda: produto, quantidade, preço unitário e desconto do
    /// item (valor absoluto). O subtotal é o valor bruto (quantidade x preço) menos o
    /// desconto, nunca negativo.
    /// </summary>
    public sealed class ItemDePedidoDeVenda
        : ObjetoDeValor<PropriedadesItemDePedidoDeVenda, IConfiguracaoObjetoDeValor>
    {
        private ItemDePedidoDeVenda(
            PropriedadesItemDePedidoDeVenda valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<ItemDePedidoDeVenda> TentarCriar(
            Id idProduto,
            Quantidade quantidade,
            Dinheiro precoUnitario,
            Dinheiro desconto,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var erros = new List<string>();

            if (idProduto is null)
                erros.Add("PRODUTO_OBRIGATORIO");

            if (quantidade is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (precoUnitario is null)
                erros.Add("PRECO_UNITARIO_OBRIGATORIO");

            if (desconto is null)
                erros.Add("DESCONTO_OBRIGATORIO");

            if (erros.Count > 0)
                return Resultado<ItemDePedidoDeVenda>.Falha(erros);

            var valorBruto = quantidade!.Valor * precoUnitario!.Valor;

            if (desconto!.Valor > valorBruto)
                return Resultado<ItemDePedidoDeVenda>.Falha("DESCONTO_ITEM_INVALIDO");

            var propriedades = new PropriedadesItemDePedidoDeVenda(
                IdProduto: idProduto!.Valor,
                Quantidade: quantidade.Valor,
                PrecoUnitario: precoUnitario.Valor,
                Desconto: desconto.Valor);

            return Resultado<ItemDePedidoDeVenda>.Sucesso(
                new ItemDePedidoDeVenda(propriedades, configuracao));
        }

        public long IdProduto => Valor.IdProduto;
        public decimal Quantidade => Valor.Quantidade;
        public decimal PrecoUnitario => Valor.PrecoUnitario;
        public decimal Desconto => Valor.Desconto;

        public decimal ValorBruto => Valor.Quantidade * Valor.PrecoUnitario;
        public decimal Subtotal => ValorBruto - Valor.Desconto;

        public bool RefereProduto(long idProduto) => Valor.IdProduto == idProduto;

        public override string ToString()
        {
            return $"Produto[{IdProduto}] x {Quantidade:0.####} @ {PrecoUnitario:0.00} - desc {Desconto:0.00}";
        }
    }

    public record PropriedadesItemDePedidoDeVenda(
        long IdProduto,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Desconto);
}
