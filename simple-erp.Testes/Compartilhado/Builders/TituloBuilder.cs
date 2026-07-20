using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System;

namespace simple_erp.Testes.Compartilhado.Builders
{
    public sealed class TituloBuilder
    {
        private long? _id = 202604020600;
        private TipoDeTitulo _tipo = TipoDeTitulo.APagar;
        private long _idParceiro = 202604020002;
        private TipoOrigemTitulo _origemTipo = TipoOrigemTitulo.Compra;
        private long? _origemIdReferencia = 202604020003;
        private decimal _valorOriginal = 100.00m;
        private DateTime _dataVencimento = DateTime.UtcNow.AddDays(30);
        private decimal _baixaInicial = 0m;

        public static TituloBuilder Novo() => new();

        public TituloBuilder ComId(long id)
        {
            _id = id;
            return this;
        }

        public TituloBuilder ComoAPagar()
        {
            _tipo = TipoDeTitulo.APagar;
            _origemTipo = TipoOrigemTitulo.Compra;
            return this;
        }

        public TituloBuilder ComoAReceber()
        {
            _tipo = TipoDeTitulo.AReceber;
            _origemTipo = TipoOrigemTitulo.Venda;
            return this;
        }

        public TituloBuilder ComIdParceiro(long idParceiro)
        {
            _idParceiro = idParceiro;
            return this;
        }

        public TituloBuilder ComValorOriginal(decimal valor)
        {
            _valorOriginal = valor;
            return this;
        }

        public TituloBuilder ComVencimento(DateTime dataVencimento)
        {
            _dataVencimento = dataVencimento;
            return this;
        }

        public TituloBuilder ComBaixaInicial(decimal valor)
        {
            _baixaInicial = valor;
            return this;
        }

        public Titulo Criar()
        {
            var idParceiro = Id.TentarCriar(_idParceiro).Instancia;
            var origem = OrigemDoTitulo.TentarCriar(_origemTipo, _origemIdReferencia).Instancia;
            var valorOriginal = Dinheiro.TentarCriar(_valorOriginal).Instancia;

            var resultado = Titulo.Criar(_tipo, idParceiro, origem, valorOriginal, _dataVencimento, _id);

            if (resultado.EhFalha)
                throw new InvalidOperationException(
                    $"Não foi possível criar Titulo válido para o teste. Erros: {string.Join(", ", resultado.Erros!)}");

            var titulo = resultado.Instancia;

            if (_baixaInicial > 0m)
            {
                var resultadoBaixa = titulo.Baixar(Dinheiro.TentarCriar(_baixaInicial).Instancia);

                if (resultadoBaixa.EhFalha)
                    throw new InvalidOperationException(
                        $"Falha ao aplicar baixa inicial no builder: {string.Join(", ", resultadoBaixa.Erros!)}");
            }

            titulo.LimparEventosDeDominio();

            return titulo;
        }
    }
}
