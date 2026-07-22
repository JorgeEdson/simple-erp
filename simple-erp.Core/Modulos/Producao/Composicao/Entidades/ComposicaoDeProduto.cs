using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Eventos;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Core.Modulos.Producao.Composicao.Entidades
{
   
    public sealed class ComposicaoDeProduto : Entidade<ComposicaoDeProduto>
    {
        
        private List<ItemDeComposicao> _itens;

        private ComposicaoDeProduto()
        {
            _itens = new List<ItemDeComposicao>();
        }


        private ComposicaoDeProduto(
            Id idProdutoFabricado,
            int versao,
            bool ativa,
            IEnumerable<ItemDeComposicao> itens,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            IdProdutoFabricado = idProdutoFabricado;
            Versao = versao;
            Ativa = ativa;
            _itens = itens.ToList();
        }

        public Id IdProdutoFabricado { get; private set; }
        public int Versao { get; private set; }
        public bool Ativa { get; private set; }

        public IReadOnlyCollection<ItemDeComposicao> Itens => _itens.AsReadOnly();

        public static Resultado<ComposicaoDeProduto> Criar(
            Id idProdutoFabricado,
            int versao,
            IEnumerable<ItemDeComposicao> itens,
            long? id = null)
        {
            if (idProdutoFabricado is null)
                return Resultado<ComposicaoDeProduto>.Falha("PRODUTO_FABRICADO_OBRIGATORIO");

            if (versao <= 0)
                return Resultado<ComposicaoDeProduto>.Falha("VERSAO_COMPOSICAO_INVALIDA");

            var listaItens = itens?.ToList() ?? new List<ItemDeComposicao>();

            if (listaItens.Count == 0)
                return Resultado<ComposicaoDeProduto>.Falha("COMPOSICAO_SEM_ITENS");

            if (listaItens.Any(item => item is null))
                return Resultado<ComposicaoDeProduto>.Falha("ITEM_COMPOSICAO_OBRIGATORIO");

            var possuiInsumoRepetido = listaItens
                .GroupBy(item => item.IdInsumo)
                .Any(grupo => grupo.Count() > 1);

            if (possuiInsumoRepetido)
                return Resultado<ComposicaoDeProduto>.Falha("INSUMO_REPETIDO_NA_COMPOSICAO");

            var composicao = new ComposicaoDeProduto(
                idProdutoFabricado,
                versao,
                ativa: false,
                listaItens,
                id);

            composicao.AdicionarEventoDeDominio(
                new ComposicaoDeProdutoCriada(composicao.Id, idProdutoFabricado, versao));

            return Resultado<ComposicaoDeProduto>.Sucesso(composicao);
        }

        public Resultado<bool> Ativar()
        {
            if (Ativa)
                return Resultado<bool>.Sucesso(true);

            Ativa = true;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(
                new ComposicaoDeProdutoAtivada(Id, IdProdutoFabricado, Versao));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Inativar()
        {
            if (!Ativa)
                return Resultado<bool>.Sucesso(true);

            Ativa = false;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(
                new ComposicaoDeProdutoInativada(Id, IdProdutoFabricado, Versao));

            return Resultado<bool>.Sucesso(true);
        }

        /// <summary>
        /// Calcula a necessidade total de cada insumo para produzir a quantidade
        /// informada. Só é permitido a partir de uma receita ativa — o que materializa
        /// a regra "garantir que exista uma receita ativa para permitir produção".
        /// </summary>
        public Resultado<IReadOnlyCollection<NecessidadeCalculada>> CalcularNecessidades(
            Quantidade quantidadeAProduzir)
        {
            if (quantidadeAProduzir is null)
                return Resultado<IReadOnlyCollection<NecessidadeCalculada>>.Falha("QUANTIDADE_OBRIGATORIA");

            if (!Ativa)
                return Resultado<IReadOnlyCollection<NecessidadeCalculada>>.Falha("COMPOSICAO_NAO_ESTA_ATIVA");

            IReadOnlyCollection<NecessidadeCalculada> necessidades = _itens
                .Select(item => new NecessidadeCalculada(
                    item.IdInsumo,
                    item.QuantidadePorUnidade * quantidadeAProduzir.Valor))
                .ToList();

            return Resultado<IReadOnlyCollection<NecessidadeCalculada>>.Sucesso(necessidades);
        }

        public static ComposicaoDeProduto Reconstituir(
            Id idProdutoFabricado,
            int versao,
            bool ativa,
            IEnumerable<ItemDeComposicao> itens,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new ComposicaoDeProduto(
                idProdutoFabricado,
                versao,
                ativa,
                itens,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
