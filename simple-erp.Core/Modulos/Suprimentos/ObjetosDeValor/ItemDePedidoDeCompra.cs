using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.Suprimentos.ObjetosDeValor
{
    public sealed class ItemDePedidoDeCompra
        : ObjetoDeValor<PropriedadesItemDePedidoDeCompra, IConfiguracaoObjetoDeValor>
    {
        private ItemDePedidoDeCompra(
            PropriedadesItemDePedidoDeCompra valor,
            IConfiguracaoObjetoDeValor? configuracao = null)
            : base(valor, configuracao)
        {
        }

        public static Resultado<ItemDePedidoDeCompra> TentarCriar(
            Id idProduto,
            Quantidade quantidade,
            Dinheiro custoUnitario,
            IConfiguracaoObjetoDeValor? configuracao = null)
        {
            var erros = new List<string>();

            if (idProduto is null)
                erros.Add("PRODUTO_OBRIGATORIO");

            if (quantidade is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (custoUnitario is null)
                erros.Add("CUSTO_UNITARIO_OBRIGATORIO");

            if (erros.Count > 0)
                return Resultado<ItemDePedidoDeCompra>.Falha(erros);

            var propriedades = new PropriedadesItemDePedidoDeCompra(
                IdProduto: idProduto!.Valor,
                Quantidade: quantidade!.Valor,
                CustoUnitario: custoUnitario!.Valor);

            return Resultado<ItemDePedidoDeCompra>.Sucesso(
                new ItemDePedidoDeCompra(propriedades, configuracao));
        }

        public long IdProduto => Valor.IdProduto;
        public decimal Quantidade => Valor.Quantidade;
        public decimal CustoUnitario => Valor.CustoUnitario;
        public decimal Subtotal => Valor.Quantidade * Valor.CustoUnitario;

        public bool RefereProduto(long idProduto) => Valor.IdProduto == idProduto;

        public override string ToString()
        {
            return $"Produto[{IdProduto}] x {Quantidade:0.####} @ {CustoUnitario:0.00}";
        }
    }

    public record PropriedadesItemDePedidoDeCompra(
        long IdProduto,
        decimal Quantidade,
        decimal CustoUnitario);
}
