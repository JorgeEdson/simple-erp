using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Interfaces.Repositorios;

namespace simple_erp.Core.Modulos.Estoque.Servicos
{
    /// <summary>
    /// Implementação do serviço de domínio de disponibilidade. Depende apenas do
    /// repositório de saldos (contrato de domínio) — nenhuma preocupação de aplicação
    /// (transação, log, DTO) vive aqui; isso é responsabilidade do caso de uso que o invoca.
    /// </summary>
    public sealed class ServicoDeDisponibilidadeDeEstoque : IServicoDeDisponibilidadeDeEstoque
    {
        private readonly ISaldoDeEstoqueRepository _saldosDeEstoqueRepository;

        public ServicoDeDisponibilidadeDeEstoque(ISaldoDeEstoqueRepository saldosDeEstoqueRepository)
        {
            _saldosDeEstoqueRepository = saldosDeEstoqueRepository;
        }

        public async Task<Resultado<VerificacaoDeDisponibilidade>> VerificarDisponibilidadeAsync(
            IEnumerable<RequisicaoDeDisponibilidade> requisicoes,
            CancellationToken cancellationToken = default)
        {
            var insuficiencias = new List<InsuficienciaDeEstoque>();

            foreach (var requisicao in requisicoes)
            {
                var disponivel = 0m;

                var existeSaldo = await _saldosDeEstoqueRepository
                    .ExistePorProdutoAsync(requisicao.IdProduto, cancellationToken);

                if (existeSaldo.EhFalha)
                    return Resultado<VerificacaoDeDisponibilidade>.Falha(existeSaldo.Erros!);

                if (existeSaldo.Instancia)
                {
                    var resultadoSaldo = await _saldosDeEstoqueRepository
                        .ObterPorProdutoAsync(requisicao.IdProduto, cancellationToken);

                    if (resultadoSaldo.EhFalha)
                        return Resultado<VerificacaoDeDisponibilidade>.Falha(resultadoSaldo.Erros!);

                    disponivel = resultadoSaldo.Instancia!.QuantidadeAtual;
                }

                if (disponivel < requisicao.QuantidadeRequerida)
                {
                    insuficiencias.Add(new InsuficienciaDeEstoque(
                        IdProduto: requisicao.IdProduto,
                        QuantidadeRequerida: requisicao.QuantidadeRequerida,
                        QuantidadeDisponivel: disponivel));
                }
            }

            return Resultado<VerificacaoDeDisponibilidade>.Sucesso(
                new VerificacaoDeDisponibilidade(insuficiencias));
        }
    }
}
