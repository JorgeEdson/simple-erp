using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Entidades;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class OrdemDeProducaoBuilder
    {
        private long? _id = 202604020400;
        private long _idProdutoFabricado = 202604020001;
        private long _idComposicao = 202604020300;
        private decimal _quantidadeAProduzir = 5m;

        private readonly List<(long IdInsumo, decimal Quantidade)> _necessidades = new()
        {
            (202604020010, 10m),
            (202604020011, 15m)
        };

        private StatusOrdemDeProducao _statusAlvo = StatusOrdemDeProducao.Criada;

        public static OrdemDeProducaoBuilder Novo() => new();

        public OrdemDeProducaoBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public OrdemDeProducaoBuilder ComIdProdutoFabricado(long id)
        {
            _idProdutoFabricado = id;
            return this;
        }

        public OrdemDeProducaoBuilder ComIdComposicao(long id)
        {
            _idComposicao = id;
            return this;
        }

        public OrdemDeProducaoBuilder ComQuantidadeAProduzir(decimal quantidade)
        {
            _quantidadeAProduzir = quantidade;
            return this;
        }

        public OrdemDeProducaoBuilder SemNecessidades()
        {
            _necessidades.Clear();
            return this;
        }

        public OrdemDeProducaoBuilder ComNecessidade(long idInsumo, decimal quantidade)
        {
            _necessidades.Add((idInsumo, quantidade));
            return this;
        }

        public OrdemDeProducaoBuilder Criada()
        {
            _statusAlvo = StatusOrdemDeProducao.Criada;
            return this;
        }

        public OrdemDeProducaoBuilder Confirmada()
        {
            _statusAlvo = StatusOrdemDeProducao.Confirmada;
            return this;
        }

        public OrdemDeProducaoBuilder Concluida()
        {
            _statusAlvo = StatusOrdemDeProducao.Concluida;
            return this;
        }

        public OrdemDeProducaoBuilder Cancelada()
        {
            _statusAlvo = StatusOrdemDeProducao.Cancelada;
            return this;
        }

        public OrdemDeProducao Criar()
        {
            var idProduto = Id.TentarCriar(_idProdutoFabricado).Instancia;
            var idComposicao = Id.TentarCriar(_idComposicao).Instancia;
            var quantidade = Quantidade.TentarCriar(_quantidadeAProduzir).Instancia;

            var necessidades = _necessidades
                .Select(necessidade => NecessidadeDeMateriaPrima.TentarCriar(
                    Id.TentarCriar(necessidade.IdInsumo).Instancia,
                    Quantidade.TentarCriar(necessidade.Quantidade).Instancia).Instancia)
                .ToList();

            var resultado = OrdemDeProducao.Criar(idProduto, idComposicao, quantidade, necessidades, _id);

            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar OrdemDeProducao válida para o teste. Erros: {string.Join(", ", resultado.Erros!)}");

            var ordem = resultado.Instancia;

            switch (_statusAlvo)
            {
                case StatusOrdemDeProducao.Criada:
                    break;
                case StatusOrdemDeProducao.Confirmada:
                    ordem.Confirmar();
                    break;
                case StatusOrdemDeProducao.Concluida:
                    ordem.Confirmar();
                    ordem.Concluir();
                    break;
                case StatusOrdemDeProducao.Cancelada:
                    ordem.Cancelar();
                    break;
            }

            ordem.LimparEventosDeDominio();

            return ordem;
        }
    }
}
