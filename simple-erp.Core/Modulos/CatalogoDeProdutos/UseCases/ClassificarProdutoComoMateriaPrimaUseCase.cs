using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IClassificarProdutoComoMateriaPrimaUseCase
        : IUseCase<ClassificarProdutoComoMateriaPrimaEntrada, ClassificarProdutoComoMateriaPrimaSaida>
    {
    }

    public sealed record ClassificarProdutoComoMateriaPrimaEntrada(long Id) : IRequisicao<ClassificarProdutoComoMateriaPrimaSaida>;

    public sealed record ClassificarProdutoComoMateriaPrimaSaida(
        long Id,
        string Classificacao,
        bool Ativo);

    public sealed class ClassificarProdutoComoMateriaPrimaUseCase : IClassificarProdutoComoMateriaPrimaUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ClassificarProdutoComoMateriaPrimaUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ClassificarProdutoComoMateriaPrimaSaida>> ExecutarAsync(ClassificarProdutoComoMateriaPrimaEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ClassificarProdutoComoMateriaPrimaUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando classificação de produto como Matéria-Prima."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para classificação de produto como Matéria-Prima.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha(resultadoId.Erros!);
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
                    Mensagem: "Falha ao obter produto por id para classificação como Matéria-Prima.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de classificação como Matéria-Prima de produto não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Classificação do produto como Matéria-Prima

                var stopwatchClassificacao = Stopwatch.StartNew();

                var resultadoClassificacao = produto.ClassificarComoMateriaPrima();

                stopwatchClassificacao.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Classificação do agregado Produto como Matéria-Prima concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoDominio"] = "Produto.ClassificarComoMateriaPrima",
                        ["DuracaoMs"] = stopwatchClassificacao.ElapsedMilliseconds
                    }));

                if (resultadoClassificacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao classificar agregado Produto como Matéria-Prima.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id.Valor,
                            ["Erros"] = resultadoClassificacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha(resultadoClassificacao.Erros!);
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
                    Mensagem: "Falha ao atualizar produto no repositório durante classificação como Matéria-Prima.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da classificação de produto como Matéria-Prima concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir classificação de produto como Matéria-Prima.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto classificado como Matéria-Prima com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id.Valor,
                    ["Classificacao"] = produto.Classificacao.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ClassificarProdutoComoMateriaPrimaSaida>.Sucesso(
                new ClassificarProdutoComoMateriaPrimaSaida(
                    Id: produto.Id.Valor,
                    Classificacao: produto.Classificacao.ToString(),
                    Ativo: produto.Ativo));

            #endregion
        }
    }
}
