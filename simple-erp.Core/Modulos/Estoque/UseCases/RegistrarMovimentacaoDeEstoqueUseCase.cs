using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Estoque.Entidades;
using simple_erp.Core.Modulos.Estoque.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Estoque.UseCases
{
    public interface IRegistrarMovimentacaoDeEstoqueUseCase
        : IUseCase<RegistrarMovimentacaoDeEstoqueEntrada, RegistrarMovimentacaoDeEstoqueSaida>
    {
    }

    public record RegistrarMovimentacaoDeEstoqueEntrada(
        long IdProduto,
        TipoDeMovimentacao Tipo,
        decimal Quantidade,
        TipoOrigemMovimentacao OrigemTipo,
        long? OrigemIdReferencia = null,
        bool PermitirSaldoNegativo = false) : IRequisicao<RegistrarMovimentacaoDeEstoqueSaida>;

    public record RegistrarMovimentacaoDeEstoqueSaida(
        long IdMovimentacao,
        long IdProduto,
        string Tipo,
        string Sentido,
        decimal Quantidade,
        decimal SaldoResultante);

    public sealed class RegistrarMovimentacaoDeEstoqueUseCase : IRegistrarMovimentacaoDeEstoqueUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public RegistrarMovimentacaoDeEstoqueUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<RegistrarMovimentacaoDeEstoqueSaida>> ExecutarAsync(RegistrarMovimentacaoDeEstoqueEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(RegistrarMovimentacaoDeEstoqueUseCase),
                ["IdProduto"] = dados.IdProduto,
                ["Tipo"] = dados.Tipo.ToString(),
                ["Quantidade"] = dados.Quantidade,
                ["PermitirSaldoNegativo"] = dados.PermitirSaldoNegativo
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando registro de movimentação de estoque."));

            #endregion

            #region Validação da entrada

            var resultadoIdProduto = Id.TentarCriar(dados.IdProduto);
            var resultadoQuantidade = Quantidade.TentarCriar(dados.Quantidade);
            var resultadoOrigem = OrigemDaMovimentacao.TentarCriar(dados.OrigemTipo, dados.OrigemIdReferencia);

            var validacao = Resultado.Combinar(
                resultadoIdProduto,
                resultadoQuantidade,
                resultadoOrigem);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para movimentação de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Validação de pré-condições

            var stopwatchExisteProduto = Stopwatch.StartNew();

            var produtoExiste = await _unitOfWork.ProdutosRepository.ExistePorIdAsync(
                resultadoIdProduto.Instancia,
                cancellationToken);

            stopwatchExisteProduto.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência do produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ProdutosRepository.ExistePorIdAsync",
                    ["DuracaoMs"] = stopwatchExisteProduto.ElapsedMilliseconds
                }));

            if (produtoExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência do produto para movimentação de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = produtoExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(produtoExiste.Erros!);
            }

            if (!produtoExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de movimentar estoque de produto inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdProduto"] = dados.IdProduto,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchExisteSaldo = Stopwatch.StartNew();

            var saldoExiste = await _unitOfWork.SaldosDeEstoqueRepository.ExistePorProdutoAsync(
                resultadoIdProduto.Instancia,
                cancellationToken);

            stopwatchExisteSaldo.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de saldo de estoque concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaldosDeEstoqueRepository.ExistePorProdutoAsync",
                    ["DuracaoMs"] = stopwatchExisteSaldo.ElapsedMilliseconds
                }));

            if (saldoExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência de saldo de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = saldoExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(saldoExiste.Erros!);
            }

            SaldoDeEstoque saldo;
            var saldoEhNovo = !saldoExiste.Instancia;

            if (saldoExiste.Instancia)
            {
                var resultadoSaldoObtido = await _unitOfWork.SaldosDeEstoqueRepository.ObterPorProdutoAsync(
                    resultadoIdProduto.Instancia,
                    cancellationToken);

                if (resultadoSaldoObtido.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao obter saldo de estoque para movimentação.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoSaldoObtido.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoSaldoObtido.Erros!);
                }

                saldo = resultadoSaldoObtido.Instancia!;
            }
            else
            {
                var resultadoCriarSaldo = SaldoDeEstoque.Criar(resultadoIdProduto.Instancia);

                if (resultadoCriarSaldo.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao criar saldo de estoque para o produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoCriarSaldo.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoCriarSaldo.Erros!);
                }

                saldo = resultadoCriarSaldo.Instancia;
            }

            #endregion

            #region Execução das regras de negócio

                #region Movimentação do saldo

                var resultadoMovimentacao = saldo.Movimentar(
                    dados.Tipo,
                    resultadoQuantidade.Instancia,
                    resultadoOrigem.Instancia,
                    dados.PermitirSaldoNegativo);

                if (resultadoMovimentacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao aplicar movimentação no saldo de estoque.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["IdProduto"] = dados.IdProduto,
                            ["Tipo"] = dados.Tipo.ToString(),
                            ["Erros"] = resultadoMovimentacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoMovimentacao.Erros!);
                }

                var movimentacao = resultadoMovimentacao.Instancia;

                #endregion

            #endregion

            #region Persistência

            var resultadoSaldoPersistido = saldoEhNovo
                ? await _unitOfWork.SaldosDeEstoqueRepository.AdicionarAsync(saldo, cancellationToken)
                : await _unitOfWork.SaldosDeEstoqueRepository.AtualizarAsync(saldo, cancellationToken);

            if (resultadoSaldoPersistido.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir saldo de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoRepositorio"] = saldoEhNovo ? "AdicionarAsync" : "AtualizarAsync",
                        ["Erros"] = resultadoSaldoPersistido.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoSaldoPersistido.Erros!);
            }

            var resultadoMovimentacaoPersistida = await _unitOfWork.MovimentacoesDeEstoqueRepository.AdicionarAsync(
                movimentacao,
                cancellationToken);

            if (resultadoMovimentacaoPersistida.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir movimentação de estoque.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoMovimentacaoPersistida.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoMovimentacaoPersistida.Erros!);
            }

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir movimentação de estoque no SaveChanges.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Movimentação de estoque registrada com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["IdProduto"] = saldo.IdProduto.Valor,
                    ["MovimentacaoId"] = movimentacao.Id.Valor,
                    ["Tipo"] = movimentacao.Tipo.ToString(),
                    ["SaldoResultante"] = movimentacao.SaldoResultante,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<RegistrarMovimentacaoDeEstoqueSaida>.Sucesso(
                new RegistrarMovimentacaoDeEstoqueSaida(
                    IdMovimentacao: movimentacao.Id.Valor,
                    IdProduto: saldo.IdProduto.Valor,
                    Tipo: movimentacao.Tipo.ToString(),
                    Sentido: movimentacao.Sentido.ToString(),
                    Quantidade: movimentacao.Quantidade,
                    SaldoResultante: movimentacao.SaldoResultante));

            #endregion
        }
    }
}
