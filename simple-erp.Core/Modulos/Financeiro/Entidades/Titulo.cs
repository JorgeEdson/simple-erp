using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Eventos;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace simple_erp.Core.Modulos.Financeiro.Entidades
{   
    public sealed class Titulo : Entidade<Titulo>
    {
        // Não é readonly para que o provider de persistência (EF Core) possa
        // materializar a coleção a partir da coluna jsonb pelo campo de apoio.
        private List<BaixaDoTitulo> _baixas;

#pragma warning disable CS8618 // Construtor de materialização do EF Core: as propriedades são preenchidas pelo provider.
        /// <summary>Construtor de materialização do EF Core.</summary>
        private Titulo()
        {
            _baixas = new List<BaixaDoTitulo>();
        }
#pragma warning restore CS8618

        private Titulo(
            TipoDeTitulo tipo,
            Id idParceiro,
            OrigemDoTitulo origem,
            decimal valorOriginal,
            DateTime dataVencimentoUtc,
            StatusTitulo status,
            IEnumerable<BaixaDoTitulo> baixas,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            Tipo = tipo;
            IdParceiro = idParceiro;
            Origem = origem;
            ValorOriginal = valorOriginal;
            DataVencimentoUtc = dataVencimentoUtc;
            Status = status;
            _baixas = baixas.ToList();
        }

        public TipoDeTitulo Tipo { get; private set; }
        public Id IdParceiro { get; private set; }
        public OrigemDoTitulo Origem { get; private set; }
        public decimal ValorOriginal { get; private set; }
        public DateTime DataVencimentoUtc { get; private set; }
        public StatusTitulo Status { get; private set; }

        public IReadOnlyCollection<BaixaDoTitulo> Baixas => _baixas.AsReadOnly();

        public decimal ValorBaixado => _baixas.Sum(baixa => baixa.Montante);
        public decimal SaldoDevedor => ValorOriginal - ValorBaixado;

        public bool EhAPagar => Tipo == TipoDeTitulo.APagar;
        public bool EhAReceber => Tipo == TipoDeTitulo.AReceber;

        public bool EstaEmAberto => Status == StatusTitulo.EmAberto;
        public bool EstaParcialmenteBaixado => Status == StatusTitulo.ParcialmenteBaixado;
        public bool EstaLiquidado => Status == StatusTitulo.Liquidado;
        public bool EstaCancelado => Status == StatusTitulo.Cancelado;

        public static Resultado<Titulo> Criar(
            TipoDeTitulo tipo,
            Id idParceiro,
            OrigemDoTitulo origem,
            Dinheiro valorOriginal,
            DateTime dataVencimentoUtc,
            long? id = null)
        {
            var erros = new List<string>();

            if (idParceiro is null)
                erros.Add("PARCEIRO_OBRIGATORIO");

            if (origem is null)
                erros.Add("ORIGEM_OBRIGATORIA");

            if (valorOriginal is null)
                erros.Add("VALOR_TITULO_OBRIGATORIO");

            if (erros.Count > 0)
                return Resultado<Titulo>.Falha(erros);

            if (valorOriginal!.Valor <= 0m)
                return Resultado<Titulo>.Falha("VALOR_TITULO_INVALIDO");

            var dataEmissao = DateTime.UtcNow;

            if (dataVencimentoUtc.Date < dataEmissao.Date)
                return Resultado<Titulo>.Falha("VENCIMENTO_INVALIDO");

            var titulo = new Titulo(
                tipo,
                idParceiro!,
                origem!,
                valorOriginal.Valor,
                dataVencimentoUtc,
                StatusTitulo.EmAberto,
                new List<BaixaDoTitulo>(),
                id);

            titulo.AdicionarEventoDeDominio(
                new TituloEmitido(titulo.Id, tipo, idParceiro!, valorOriginal.Valor));

            return Resultado<Titulo>.Sucesso(titulo);
        }
       
        public Resultado<bool> Baixar(Dinheiro montante)
        {
            if (montante is null)
                return Resultado<bool>.Falha("VALOR_BAIXA_OBRIGATORIO");

            if (EstaCancelado)
                return Resultado<bool>.Falha("TITULO_CANCELADO_NAO_PODE_SER_BAIXADO");

            if (EstaLiquidado)
                return Resultado<bool>.Falha("TITULO_JA_LIQUIDADO");

            if (montante.Valor <= 0m)
                return Resultado<bool>.Falha("VALOR_BAIXA_INVALIDO");

            if (montante.Valor > SaldoDevedor)
                return Resultado<bool>.Falha("VALOR_BAIXA_EXCEDE_SALDO");

            var resultadoBaixa = BaixaDoTitulo.TentarCriar(montante);

            if (resultadoBaixa.EhFalha)
                return Resultado<bool>.Falha(resultadoBaixa.Erros!);

            _baixas.Add(resultadoBaixa.Instancia);
            AtualizarDataAtualizacao();

            var liquidado = SaldoDevedor <= 0m;
            Status = liquidado ? StatusTitulo.Liquidado : StatusTitulo.ParcialmenteBaixado;

            AdicionarEventoDeDominio(new TituloBaixado(
                Id,
                montante.Valor,
                ValorBaixado,
                SaldoDevedor));

            if (liquidado)
                AdicionarEventoDeDominio(new TituloLiquidado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public Resultado<bool> Cancelar()
        {
            if (EstaCancelado)
                return Resultado<bool>.Sucesso(true);

            if (EstaLiquidado)
                return Resultado<bool>.Falha("TITULO_LIQUIDADO_NAO_PODE_SER_CANCELADO");

            Status = StatusTitulo.Cancelado;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new TituloCancelado(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public static Titulo Reconstituir(
            TipoDeTitulo tipo,
            Id idParceiro,
            OrigemDoTitulo origem,
            decimal valorOriginal,
            DateTime dataVencimentoUtc,
            StatusTitulo status,
            IEnumerable<BaixaDoTitulo> baixas,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new Titulo(
                tipo,
                idParceiro,
                origem,
                valorOriginal,
                dataVencimentoUtc,
                status,
                baixas,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
