using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Composicao.Entidades;
using simple_erp.Core.Modulos.Producao.Composicao.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class ComposicaoDeProdutoBuilder
    {
        private long? _id = 202604020300;
        private long _idProdutoFabricado = 202604020001;
        private int _versao = 1;
        private bool _ativa = false;

        private readonly List<(long IdInsumo, decimal Quantidade)> _itens = new()
        {
            (202604020010, 2m),
            (202604020011, 3m)
        };

        public static ComposicaoDeProdutoBuilder Novo() => new();

        public ComposicaoDeProdutoBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public ComposicaoDeProdutoBuilder ComIdProdutoFabricado(long id)
        {
            _idProdutoFabricado = id;
            return this;
        }

        public ComposicaoDeProdutoBuilder ComVersao(int versao)
        {
            _versao = versao;
            return this;
        }

        public ComposicaoDeProdutoBuilder Ativa()
        {
            _ativa = true;
            return this;
        }

        public ComposicaoDeProdutoBuilder SemItens()
        {
            _itens.Clear();
            return this;
        }

        public ComposicaoDeProdutoBuilder ComItem(long idInsumo, decimal quantidadePorUnidade)
        {
            _itens.Add((idInsumo, quantidadePorUnidade));
            return this;
        }

        public ComposicaoDeProduto Criar()
        {
            var idProduto = Id.TentarCriar(_idProdutoFabricado).Instancia;

            var itens = _itens
                .Select(item => ItemDeComposicao.TentarCriar(
                    Id.TentarCriar(item.IdInsumo).Instancia,
                    Quantidade.TentarCriar(item.Quantidade).Instancia).Instancia)
                .ToList();

            var resultado = ComposicaoDeProduto.Criar(idProduto, _versao, itens, _id);

            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar ComposicaoDeProduto válida para o teste. Erros: {string.Join(", ", resultado.Erros!)}");

            var composicao = resultado.Instancia;

            if (_ativa)
                composicao.Ativar();

            composicao.LimparEventosDeDominio();

            return composicao;
        }
    }
}
