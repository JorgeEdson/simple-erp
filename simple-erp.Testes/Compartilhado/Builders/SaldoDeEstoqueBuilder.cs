using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System;

namespace simple_erp.Testes.Compartilhado.Builders
{
    /// <summary>
    /// Constrói um SaldoDeEstoque para testes. O saldo inicial é atingido registrando
    /// uma entrada por ajuste (quando maior que zero), de modo que a quantidade parta
    /// de um valor conhecido sem expor setters do agregado.
    /// </summary>
    public sealed class SaldoDeEstoqueBuilder
    {
        private long? _id = 202604020100;
        private long _idProduto = 202604020001;
        private decimal _saldoInicial = 0m;

        public static SaldoDeEstoqueBuilder Novo() => new();

        public SaldoDeEstoqueBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public SaldoDeEstoqueBuilder SemId()
        {
            _id = null;
            return this;
        }

        public SaldoDeEstoqueBuilder ComIdProduto(long idProduto)
        {
            _idProduto = idProduto;
            return this;
        }

        public SaldoDeEstoqueBuilder ComSaldoInicial(decimal saldoInicial)
        {
            _saldoInicial = saldoInicial;
            return this;
        }

        public SaldoDeEstoque Criar()
        {
            var resultadoIdProduto = Id.TentarCriar(_idProduto);

            if (resultadoIdProduto.EhFalha)
                throw new InvalidOperationException(
                    $"Id de produto inválido no builder: {string.Join(", ", resultadoIdProduto.Erros!)}");

            var resultadoSaldo = SaldoDeEstoque.Criar(resultadoIdProduto.Instancia, _id);

            if (resultadoSaldo.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar SaldoDeEstoque válido para o teste. Erros: {string.Join(", ", resultadoSaldo.Erros!)}");

            var saldo = resultadoSaldo.Instancia;

            if (_saldoInicial > 0m)
            {
                var origem = OrigemDaMovimentacao.TentarCriar(TipoOrigemMovimentacao.AjusteManual).Instancia;
                var quantidade = Quantidade.TentarCriar(_saldoInicial).Instancia;

                var resultadoMovimentacao = saldo.Movimentar(
                    TipoDeMovimentacao.AjustePositivo,
                    quantidade,
                    origem);

                if (resultadoMovimentacao.EhFalha)
                    throw new InvalidOperationException(
                        $"Falha ao ajustar saldo inicial no builder: {string.Join(", ", resultadoMovimentacao.Erros!)}");
            }

            saldo.LimparEventosDeDominio();

            return saldo;
        }
    }
}
