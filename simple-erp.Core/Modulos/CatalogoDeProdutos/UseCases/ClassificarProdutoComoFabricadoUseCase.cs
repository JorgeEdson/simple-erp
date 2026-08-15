using simple_erp.Core.Compartilhado.Base;
using System.Diagnostics;
using simple_erp.Core.Compartilhado.Contratos.Aplicacao;
using simple_erp.Core.Compartilhado.Contratos.Observabilidade;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IClassificarProdutoComoFabricadoUseCase
        : IUseCase<ClassificarProdutoComoFabricadoEntrada, ClassificarProdutoComoFabricadoSaida>
    {
    }

    public sealed record ClassificarProdutoComoFabricadoEntrada(Guid Id) : IRequisicao<ClassificarProdutoComoFabricadoSaida>;

    public sealed record ClassificarProdutoComoFabricadoSaida(
        Guid Id,
        string Classificacao,
        bool Ativo);

    public sealed class ClassificarProdutoComoFabricadoUseCase : IClassificarProdutoComoFabricadoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ClassificarProdutoComoFabricadoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ClassificarProdutoComoFabricadoSaida>> ExecutarAsync(ClassificarProdutoComoFabricadoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ClassificarProdutoComoFabricadoUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando classificação de produto como Fabricado."));

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

                return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha("ID_INVALIDO");
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterProduto = Stopwatch.StartNew();

            var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                dados.Id,
                cancellationToken);

            stopwatchObterProduto.Stop();

            if (resultadoProduto.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao obter produto por id para classificação como Fabricado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de classificação como Fabricado de produto não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Classificação do produto como Fabricado

                var stopwatchClassificacao = Stopwatch.StartNew();

                var resultadoClassificacao = produto.ClassificarComoFabricado();

                stopwatchClassificacao.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Classificação do agregado Produto como Fabricado concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoDominio"] = "Produto.ClassificarComoFabricado",
                        ["DuracaoMs"] = stopwatchClassificacao.ElapsedMilliseconds
                    }));

                if (resultadoClassificacao.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao classificar agregado Produto como Fabricado.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id,
                            ["Erros"] = resultadoClassificacao.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha(resultadoClassificacao.Erros!);
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
                    Mensagem: "Falha ao atualizar produto no repositório durante classificação como Fabricado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da classificação de produto como Fabricado concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir classificação de produto como Fabricado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ClassificarProdutoComoFabricadoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto classificado como Fabricado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id,
                    ["Classificacao"] = produto.Classificacao.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ClassificarProdutoComoFabricadoSaida>.Sucesso(
                new ClassificarProdutoComoFabricadoSaida(
                    Id: produto.Id,
                    Classificacao: produto.Classificacao.ToString(),
                    Ativo: produto.Ativo));

            #endregion
        }
    }
}
