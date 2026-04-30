using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class ProdutoBuilder
    {
        private long? _id = 202604020001;
        private string _codigo = "PROD-001";
        private string _descricao = "Produto Teste";
        private string _unidadeDeMedida = "UN";
        private ClassificacaoProduto _classificacao = ClassificacaoProduto.SemClassificacao;
        private bool _ativo = true;

        public static ProdutoBuilder Novo() => new();

        public ProdutoBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public ProdutoBuilder SemId()
        {
            _id = null;
            return this;
        }

        public ProdutoBuilder ComCodigo(string codigo)
        {
            _codigo = codigo;
            return this;
        }

        public ProdutoBuilder ComDescricao(string descricao)
        {
            _descricao = descricao;
            return this;
        }

        public ProdutoBuilder ComUnidadeDeMedida(string unidadeDeMedida)
        {
            _unidadeDeMedida = unidadeDeMedida;
            return this;
        }

        public ProdutoBuilder ComClassificacao(ClassificacaoProduto classificacao)
        {
            _classificacao = classificacao;
            return this;
        }

        public ProdutoBuilder ComoFabricado()
        {
            _classificacao = ClassificacaoProduto.Fabricado;
            return this;
        }

        public ProdutoBuilder ComoMateriaPrima()
        {
            _classificacao = ClassificacaoProduto.MateriaPrima;
            return this;
        }

        public ProdutoBuilder SemClassificacao()
        {
            _classificacao = ClassificacaoProduto.SemClassificacao;
            return this;
        }

        public ProdutoBuilder Ativo()
        {
            _ativo = true;
            return this;
        }

        public ProdutoBuilder Inativo()
        {
            _ativo = false;
            return this;
        }

        public Produto Criar()
        {
            var resultadoCodigo = CodigoProduto.TentarCriar(_codigo);
            var resultadoDescricao = DescricaoProduto.TentarCriar(_descricao);
            var resultadoUnidade = UnidadeDeMedida.TentarCriar(_unidadeDeMedida);

            if (resultadoCodigo.EhFalha)
                throw new InvalidOperationException($"Código inválido no builder: {string.Join(", ", resultadoCodigo.Erros!)}");

            if (resultadoDescricao.EhFalha)
                throw new InvalidOperationException($"Descrição inválida no builder: {string.Join(", ", resultadoDescricao.Erros!)}");

            if (resultadoUnidade.EhFalha)
                throw new InvalidOperationException($"Unidade de medida inválida no builder: {string.Join(", ", resultadoUnidade.Erros!)}");

            var resultadoProduto = Produto.Criar(
                resultadoCodigo.Instancia,
                resultadoDescricao.Instancia,
                resultadoUnidade.Instancia,
                _classificacao,
                _id);

            if (resultadoProduto.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar Produto válido para o teste. Erros: {string.Join(", ", resultadoProduto.Erros!)}");

            var produto = resultadoProduto.Instancia;

            if (!_ativo)
                produto.Inativar();

            return produto;
        }
    }
}
