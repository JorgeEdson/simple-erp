using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Eventos;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades
{
    public sealed class Produto : Entidade<Produto>
    {
#pragma warning disable CS8618 // Construtor de materialização do EF Core: as propriedades são preenchidas pelo provider.
        /// <summary>Construtor de materialização do EF Core.</summary>
        private Produto()
        {
        }
#pragma warning restore CS8618

        private Produto(
            CodigoProduto codigo,
            DescricaoProduto descricao,
            UnidadeDeMedida unidadeDeMedida,
            ClassificacaoProduto classificacao,
            bool ativo = true,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            Codigo = codigo;
            Descricao = descricao;
            UnidadeDeMedida = unidadeDeMedida;
            Classificacao = classificacao;
            Ativo = ativo;
        }

        public CodigoProduto Codigo { get; private set; }
        public DescricaoProduto Descricao { get; private set; }
        public UnidadeDeMedida UnidadeDeMedida { get; private set; }
        public ClassificacaoProduto Classificacao { get; private set; }
        public bool Ativo { get; private set; }

        public bool EhFabricado => Classificacao == ClassificacaoProduto.Fabricado;
        public bool EhMateriaPrima => Classificacao == ClassificacaoProduto.MateriaPrima;
        public bool PossuiClassificacao => Classificacao != ClassificacaoProduto.SemClassificacao;

        public static Resultado<Produto> Criar(
            CodigoProduto codigo,
            DescricaoProduto descricao,
            UnidadeDeMedida unidadeDeMedida,
            ClassificacaoProduto classificacao = ClassificacaoProduto.SemClassificacao,
            long? id = null)
        {
            var erros = new List<string>();

            if (codigo is null)
                erros.Add("CODIGO_PRODUTO_OBRIGATORIO");

            if (descricao is null)
                erros.Add("DESCRICAO_PRODUTO_OBRIGATORIA");

            if (unidadeDeMedida is null)
                erros.Add("UNIDADE_MEDIDA_OBRIGATORIA");

            if (erros.Any())
                return Resultado<Produto>.Falha(erros);

            var produto = new Produto(
                codigo!,
                descricao!,
                unidadeDeMedida!,
                classificacao,
                true,
                id);

            produto.AdicionarEventoDeDominio(
                new ProdutoCadastrado(produto.Id, produto.Codigo, produto.Descricao));

            if (classificacao == ClassificacaoProduto.Fabricado)
            {
                produto.AdicionarEventoDeDominio(
                    new ProdutoClassificadoComoFabricado(produto.Id));
            }
            else if (classificacao == ClassificacaoProduto.MateriaPrima)
            {
                produto.AdicionarEventoDeDominio(
                    new ProdutoClassificadoComoMateriaPrima(produto.Id));
            }

            return Resultado<Produto>.Sucesso(produto);
        }

        public static Produto Reconstituir(
            CodigoProduto codigo,
            DescricaoProduto descricao,
            UnidadeDeMedida unidadeDeMedida,
            ClassificacaoProduto classificacao,
            bool ativo,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new Produto(
                codigo,
                descricao,
                unidadeDeMedida,
                classificacao,
                ativo,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }

        public Resultado<bool> AlterarCodigo(CodigoProduto codigo)
        {
            if (codigo is null)
                return Resultado<bool>.Falha("CODIGO_PRODUTO_OBRIGATORIO");

            if (Codigo.IgualA(codigo))
                return Resultado<bool>.Sucesso(true);

            Codigo = codigo;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AlterarDescricao(DescricaoProduto descricao)
        {
            if (descricao is null)
                return Resultado<bool>.Falha("DESCRICAO_PRODUTO_OBRIGATORIA");

            if (Descricao.IgualA(descricao))
                return Resultado<bool>.Sucesso(true);

            Descricao = descricao;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> AlterarUnidadeDeMedida(UnidadeDeMedida unidadeDeMedida)
        {
            if (unidadeDeMedida is null)
                return Resultado<bool>.Falha("UNIDADE_MEDIDA_OBRIGATORIA");

            if (UnidadeDeMedida.IgualA(unidadeDeMedida))
                return Resultado<bool>.Sucesso(true);

            UnidadeDeMedida = unidadeDeMedida;
            AtualizarDataAtualizacao();

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Ativar()
        {
            if (Ativo)
                return Resultado<bool>.Sucesso(true);

            Ativo = true;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new ProdutoReativado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Inativar()
        {
            if (!Ativo)
                return Resultado<bool>.Sucesso(true);

            Ativo = false;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new ProdutoInativado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> ClassificarComoFabricado()
        {
            if (!Ativo)
                return Resultado<bool>.Falha("PRODUTO_INATIVO_NAO_PODE_SER_CLASSIFICADO");

            if (Classificacao == ClassificacaoProduto.Fabricado)
                return Resultado<bool>.Sucesso(true);

            Classificacao = ClassificacaoProduto.Fabricado;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new ProdutoClassificadoComoFabricado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> ClassificarComoMateriaPrima()
        {
            if (!Ativo)
                return Resultado<bool>.Falha("PRODUTO_INATIVO_NAO_PODE_SER_CLASSIFICADO");

            if (Classificacao == ClassificacaoProduto.MateriaPrima)
                return Resultado<bool>.Sucesso(true);

            Classificacao = ClassificacaoProduto.MateriaPrima;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new ProdutoClassificadoComoMateriaPrima(Id));

            return Resultado<bool>.Sucesso(true);
        }
    }
}
