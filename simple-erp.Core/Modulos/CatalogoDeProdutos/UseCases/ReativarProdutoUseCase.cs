using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IReativarProdutoUseCase : IUseCase<ReativarProdutoEntrada, ReativarProdutoSaida>
    {
    }

    public sealed record ReativarProdutoEntrada(Guid Id) : IRequisicao<ReativarProdutoSaida>;

    public sealed record ReativarProdutoSaida(
       Guid Id,
       bool Ativo);

    public sealed class ReativarProdutoUseCase : IReativarProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ReativarProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ReativarProdutoSaida>> ExecutarAsync(ReativarProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ReativarProdutoUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando reativação de produto."));

            #endregion

            #region Validação do identificador

            if (dados.Id == Guid.Empty)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Identificador não informado na entrada do caso de uso.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Id"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarProdutoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterProduto = Stopwatch.StartNew();

            var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                dados.Id,
                cancellationToken);

            stopwatchObterProduto.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Consulta de produto por id concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ObterPorIdAsync",
                    ["DuracaoMs"] = stopwatchObterProduto.ElapsedMilliseconds
                }));

            if (resultadoProduto.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter produto por id para reativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarProdutoSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de reativação de produto não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarProdutoSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Reativação do produto

                var stopwatchAtivacao = Stopwatch.StartNew();

                var resultadoAtivacao = produto.Ativar();

                stopwatchAtivacao.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Reativação do agregado Produto concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoDominio"] = "Produto.Ativar",
                        ["DuracaoMs"] = stopwatchAtivacao.ElapsedMilliseconds
                    }));

                if (resultadoAtivacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao reativar agregado Produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id,
                            ["Erros"] = resultadoAtivacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<ReativarProdutoSaida>.Falha(resultadoAtivacao.Erros!);
                }

                #endregion

            #endregion

            #region Persistência

            var stopwatchAtualizar = Stopwatch.StartNew();

            var resultadoAtualizar = await _unitOfWork.ProdutosRepository.AtualizarAsync(
                produto,
                cancellationToken);

            stopwatchAtualizar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Atualização de produto no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AtualizarAsync",
                    ["DuracaoMs"] = stopwatchAtualizar.ElapsedMilliseconds
                }));

            if (resultadoAtualizar.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao atualizar produto no repositório durante reativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarProdutoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da reativação de produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir reativação de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ReativarProdutoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto reativado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id,
                    ["Ativo"] = produto.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ReativarProdutoSaida>.Sucesso(
                new ReativarProdutoSaida(
                    Id: produto.Id,
                    Ativo: produto.Ativo));

            #endregion
        }
    }
}
