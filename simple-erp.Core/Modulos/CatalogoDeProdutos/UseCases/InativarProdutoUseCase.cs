using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IInativarProdutoUseCase : IUseCase<InativarProdutoEntrada, InativarProdutoSaida>
    {
    }

    public record InativarProdutoEntrada(long Id);

    public record InativarProdutoSaida(
       long Id,
       bool Ativo
    );

    public sealed class InativarProdutoUseCase : IInativarProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public InativarProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<InativarProdutoSaida>> ExecutarAsync(InativarProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(InativarProdutoUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando inativação de produto."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para inativação de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarProdutoSaida>.Falha(resultadoId.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterProduto = Stopwatch.StartNew();

            var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            stopwatchObterProduto.Stop();

            if (resultadoProduto.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter produto por id para inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarProdutoSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de inativação de produto não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarProdutoSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Inativação do produto

                var stopwatchInativacao = Stopwatch.StartNew();

                var resultadoInativacao = produto.Inativar();

                stopwatchInativacao.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Inativação do agregado Produto concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoDominio"] = "Produto.Inativar",
                        ["DuracaoMs"] = stopwatchInativacao.ElapsedMilliseconds
                    }));

                if (resultadoInativacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao inativar agregado Produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id.Valor,
                            ["Erros"] = resultadoInativacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<InativarProdutoSaida>.Falha(resultadoInativacao.Erros!);
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
                    Mensagem: "Falha ao atualizar produto no repositório durante inativação.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarProdutoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da inativação de produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir inativação de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<InativarProdutoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto inativado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id.Valor,
                    ["Ativo"] = produto.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<InativarProdutoSaida>.Sucesso(
                new InativarProdutoSaida(
                    Id: produto.Id.Valor,
                    Ativo: produto.Ativo));

            #endregion
        }
    }
}
