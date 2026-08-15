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
        private Guid? _id = new Guid("00000000-0000-0000-0000-202604020300");
        private Guid _idProdutoFabricado = new Guid("00000000-0000-0000-0000-202604020001");
        private int _versao = 1;
        private bool _ativa = false;

        private readonly List<(Guid IdInsumo, decimal Quantidade)> _itens = new()
        {
            (new Guid("00000000-0000-0000-0000-202604020010"), 2m),
            (new Guid("00000000-0000-0000-0000-202604020011"), 3m)
        };

        public static ComposicaoDeProdutoBuilder Novo() => new();

        public ComposicaoDeProdutoBuilder ComId(Guid id)
        {
            _id = id;
            return this;
        }

        public ComposicaoDeProdutoBuilder ComIdProdutoFabricado(Guid id)
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

        public ComposicaoDeProdutoBuilder ComItem(Guid idInsumo, decimal quantidadePorUnidade)
        {
            _itens.Add((idInsumo, quantidadePorUnidade));
            return this;
        }

        public ComposicaoDeProduto Criar()
        {
            var idProduto = _idProdutoFabricado;

            var itens = _itens
                .Select(item => ItemDeComposicao.TentarCriar(
                    item.IdInsumo,
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
