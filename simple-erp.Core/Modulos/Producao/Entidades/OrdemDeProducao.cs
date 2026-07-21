using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Producao.Eventos;
using simple_erp.Core.Modulos.Producao.ObjetosDeValor;

namespace simple_erp.Core.Modulos.Producao.Entidades
{
  
    public sealed class OrdemDeProducao : Entidade<OrdemDeProducao>
    {
        // Não é readonly para que o provider de persistência (EF Core) possa
        // materializar a coleção a partir da coluna jsonb pelo campo de apoio.
        private List<NecessidadeDeMateriaPrima> _necessidades;

#pragma warning disable CS8618 // Construtor de materialização do EF Core: as propriedades são preenchidas pelo provider.
        /// <summary>Construtor de materialização do EF Core.</summary>
        private OrdemDeProducao()
        {
            _necessidades = new List<NecessidadeDeMateriaPrima>();
        }
#pragma warning restore CS8618

        private OrdemDeProducao(
            Id idProdutoFabricado,
            Id idComposicao,
            decimal quantidadeAProduzir,
            StatusOrdemDeProducao status,
            IEnumerable<NecessidadeDeMateriaPrima> necessidades,
            long? id = null,
            DateTime? dataCriacaoUtc = null,
            DateTime? dataAtualizacaoUtc = null)
            : base(id, dataCriacaoUtc, dataAtualizacaoUtc)
        {
            IdProdutoFabricado = idProdutoFabricado;
            IdComposicao = idComposicao;
            QuantidadeAProduzir = quantidadeAProduzir;
            Status = status;
            _necessidades = necessidades.ToList();
        }

        public Id IdProdutoFabricado { get; private set; }
        public Id IdComposicao { get; private set; }
        public decimal QuantidadeAProduzir { get; private set; }
        public StatusOrdemDeProducao Status { get; private set; }

        public IReadOnlyCollection<NecessidadeDeMateriaPrima> Necessidades => _necessidades.AsReadOnly();

        public bool EstaCriada => Status == StatusOrdemDeProducao.Criada;
        public bool EstaConfirmada => Status == StatusOrdemDeProducao.Confirmada;
        public bool EstaConcluida => Status == StatusOrdemDeProducao.Concluida;
        public bool EstaCancelada => Status == StatusOrdemDeProducao.Cancelada;

        public static Resultado<OrdemDeProducao> Criar(
            Id idProdutoFabricado,
            Id idComposicao,
            Quantidade quantidadeAProduzir,
            IEnumerable<NecessidadeDeMateriaPrima> necessidades,
            long? id = null)
        {
            var erros = new List<string>();

            if (idProdutoFabricado is null)
                erros.Add("PRODUTO_FABRICADO_OBRIGATORIO");

            if (idComposicao is null)
                erros.Add("COMPOSICAO_OBRIGATORIA");

            if (quantidadeAProduzir is null)
                erros.Add("QUANTIDADE_OBRIGATORIA");

            if (erros.Count > 0)
                return Resultado<OrdemDeProducao>.Falha(erros);

            var listaNecessidades = necessidades?.ToList() ?? new List<NecessidadeDeMateriaPrima>();

            if (listaNecessidades.Count == 0)
                return Resultado<OrdemDeProducao>.Falha("NECESSIDADES_OBRIGATORIAS");

            if (listaNecessidades.Any(necessidade => necessidade is null))
                return Resultado<OrdemDeProducao>.Falha("NECESSIDADE_OBRIGATORIA");

            var possuiInsumoRepetido = listaNecessidades
                .GroupBy(necessidade => necessidade.IdInsumo)
                .Any(grupo => grupo.Count() > 1);

            if (possuiInsumoRepetido)
                return Resultado<OrdemDeProducao>.Falha("INSUMO_REPETIDO_NA_ORDEM");

            var ordem = new OrdemDeProducao(
                idProdutoFabricado!,
                idComposicao!,
                quantidadeAProduzir!.Valor,
                StatusOrdemDeProducao.Criada,
                listaNecessidades,
                id);

            ordem.AdicionarEventoDeDominio(new OrdemDeProducaoCriada(
                ordem.Id,
                idProdutoFabricado!,
                idComposicao!,
                quantidadeAProduzir!.Valor));

            return Resultado<OrdemDeProducao>.Sucesso(ordem);
        }

       
        public Resultado<bool> Confirmar()
        {
            if (EstaConfirmada)
                return Resultado<bool>.Sucesso(true);

            if (!EstaCriada)
                return Resultado<bool>.Falha("ORDEM_DE_PRODUCAO_NAO_PODE_SER_CONFIRMADA");

            Status = StatusOrdemDeProducao.Confirmada;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new OrdemDeProducaoConfirmada(Id, IdProdutoFabricado));

            return Resultado<bool>.Sucesso(true);
        }

       
        public Resultado<bool> Concluir()
        {
            if (EstaConcluida)
                return Resultado<bool>.Sucesso(true);

            if (!EstaConfirmada)
                return Resultado<bool>.Falha("ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");

            Status = StatusOrdemDeProducao.Concluida;
            AtualizarDataAtualizacao();

            var insumos = _necessidades
                .Select(necessidade => new InsumoConsumido(
                    necessidade.IdInsumo,
                    necessidade.QuantidadeNecessaria))
                .ToList();

            AdicionarEventoDeDominio(new OrdemDeProducaoConcluida(
                Id,
                IdProdutoFabricado,
                QuantidadeAProduzir,
                insumos));

            return Resultado<bool>.Sucesso(true);
        }

     
        public Resultado<bool> Cancelar()
        {
            if (EstaCancelada)
                return Resultado<bool>.Sucesso(true);

            if (EstaConcluida)
                return Resultado<bool>.Falha("ORDEM_DE_PRODUCAO_CONCLUIDA_NAO_PODE_SER_CANCELADA");

            Status = StatusOrdemDeProducao.Cancelada;
            AtualizarDataAtualizacao();

            AdicionarEventoDeDominio(new OrdemDeProducaoCancelada(Id));

            return Resultado<bool>.Sucesso(true);
        }

        public static OrdemDeProducao Reconstituir(
            Id idProdutoFabricado,
            Id idComposicao,
            decimal quantidadeAProduzir,
            StatusOrdemDeProducao status,
            IEnumerable<NecessidadeDeMateriaPrima> necessidades,
            long id,
            DateTime dataCriacaoUtc,
            DateTime dataAtualizacaoUtc)
        {
            return new OrdemDeProducao(
                idProdutoFabricado,
                idComposicao,
                quantidadeAProduzir,
                status,
                necessidades,
                id,
                dataCriacaoUtc,
                dataAtualizacaoUtc);
        }
    }
}
