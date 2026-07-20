using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.UseCases;
using simple_erp.Core.Modulos.Producao.Entidades;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Producao.UseCases
{
    public interface IConcluirOrdemDeProducaoUseCase
        : IUseCase<ConcluirOrdemDeProducaoEntrada, ConcluirOrdemDeProducaoSaida>
    {
    }

    public record ConcluirOrdemDeProducaoEntrada(long Id);

    public record ConcluirOrdemDeProducaoSaida(
        long Id,
        string Status,
        long IdProdutoFabricado,
        decimal QuantidadeProduzida,
        int QuantidadeInsumosConsumidos);

    public sealed class ConcluirOrdemDeProducaoUseCase : IConcluirOrdemDeProducaoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;
        private readonly IRegistrarMovimentacaoDeEstoqueUseCase _registrarMovimentacao;

        public ConcluirOrdemDeProducaoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService,
            IRegistrarMovimentacaoDeEstoqueUseCase registrarMovimentacao)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
            _registrarMovimentacao = registrarMovimentacao;
        }

        public async Task<Resultado<ConcluirOrdemDeProducaoSaida>> ExecutarAsync(ConcluirOrdemDeProducaoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ConcluirOrdemDeProducaoUseCase),
                ["OrdemDeProducaoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando conclusão de ordem de produção."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var resultadoOrdem = await _unitOfWork.OrdensDeProducaoRepository
                .ObterPorIdAsync(resultadoId.Instancia, cancellationToken);

            if (resultadoOrdem.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoOrdem.Erros!);
            }

            var ordem = resultadoOrdem.Instancia;

            if (ordem is null)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de concluir ordem de produção não encontrada.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OrdemDeProducaoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha("ORDEM_DE_PRODUCAO_NAO_ENCONTRADA");
            }

            #endregion

            #region Validação de pré-condições

            if (ordem.EstaConcluida)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));
            }

            if (!ordem.EstaConfirmada)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(
                    "ORDEM_DE_PRODUCAO_NAO_CONFIRMADA_NAO_PODE_SER_CONCLUIDA");
            }

            #endregion

            #region Execução das regras de negócio

                #region Baixa das matérias-primas (saída por produção)

                var resultadoBaixas = await RegistrarBaixasDeMateriaPrimaAsync(ordem, cancellationToken);

                if (resultadoBaixas.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao dar baixa das matérias-primas na conclusão da ordem.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["OrdemDeProducaoId"] = ordem.Id.Valor,
                            ["Erros"] = resultadoBaixas.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoBaixas.Erros!);
                }

                #endregion

                #region Entrada do produto acabado (entrada por produção)

                var resultadoEntrada = await RegistrarEntradaDoProdutoAcabadoAsync(ordem, cancellationToken);

                if (resultadoEntrada.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao dar entrada do produto acabado na conclusão da ordem.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["OrdemDeProducaoId"] = ordem.Id.Valor,
                            ["Erros"] = resultadoEntrada.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoEntrada.Erros!);
                }

                #endregion

                #region Conclusão da ordem de produção

                var resultadoConcluir = ordem.Concluir();

                if (resultadoConcluir.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoConcluir.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var resultadoAtualizar = await _unitOfWork.OrdensDeProducaoRepository
                .AtualizarAsync(ordem, cancellationToken);

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir conclusão de ordem de produção.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<ConcluirOrdemDeProducaoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Ordem de produção concluída com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OrdemDeProducaoId"] = ordem.Id.Valor,
                    ["Status"] = ordem.Status.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ConcluirOrdemDeProducaoSaida>.Sucesso(Mapear(ordem));

            #endregion
        }

        private async Task<Resultado<bool>> RegistrarBaixasDeMateriaPrimaAsync(
            OrdemDeProducao ordem,
            CancellationToken cancellationToken)
        {
            foreach (var necessidade in ordem.Necessidades)
            {
                var entrada = new RegistrarMovimentacaoDeEstoqueEntrada(
                    IdProduto: necessidade.IdInsumo,
                    Tipo: TipoDeMovimentacao.SaidaPorProducao,
                    Quantidade: necessidade.QuantidadeNecessaria,
                    OrigemTipo: TipoOrigemMovimentacao.Producao,
                    OrigemIdReferencia: ordem.Id.Valor,
                    PermitirSaldoNegativo: false);

                var resultado = await _registrarMovimentacao.ExecutarAsync(entrada, cancellationToken);

                if (resultado.EhFalha)
                    return Resultado<bool>.Falha(resultado.Erros!);
            }

            return Resultado<bool>.Sucesso(true);
        }

        private async Task<Resultado<bool>> RegistrarEntradaDoProdutoAcabadoAsync(
            OrdemDeProducao ordem,
            CancellationToken cancellationToken)
        {
            var entrada = new RegistrarMovimentacaoDeEstoqueEntrada(
                IdProduto: ordem.IdProdutoFabricado.Valor,
                Tipo: TipoDeMovimentacao.EntradaPorProducao,
                Quantidade: ordem.QuantidadeAProduzir,
                OrigemTipo: TipoOrigemMovimentacao.Producao,
                OrigemIdReferencia: ordem.Id.Valor,
                PermitirSaldoNegativo: false);

            var resultado = await _registrarMovimentacao.ExecutarAsync(entrada, cancellationToken);

            if (resultado.EhFalha)
                return Resultado<bool>.Falha(resultado.Erros!);

            return Resultado<bool>.Sucesso(true);
        }

        private static ConcluirOrdemDeProducaoSaida Mapear(OrdemDeProducao ordem)
        {
            return new ConcluirOrdemDeProducaoSaida(
                Id: ordem.Id.Valor,
                Status: ordem.Status.ToString(),
                IdProdutoFabricado: ordem.IdProdutoFabricado.Valor,
                QuantidadeProduzida: ordem.QuantidadeAProduzir,
                QuantidadeInsumosConsumidos: ordem.Necessidades.Count);
        }
    }
}
