using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IEditarProdutoUseCase
         : IUseCase<EditarProdutoEntrada, EditarProdutoSaida>
    {
    }

    public record EditarProdutoEntrada(
        long Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida
    );

    public record EditarProdutoSaida(
        long Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string Classificacao,
        bool Ativo
    );

    public sealed class EditarProdutoUseCase : IEditarProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public EditarProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<EditarProdutoSaida>> ExecutarAsync(EditarProdutoEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(EditarProdutoUseCase),
                ["ProdutoId"] = dados.Id,
                ["Codigo"] = dados.Codigo,
                ["Descricao"] = dados.Descricao,
                ["UnidadeDeMedida"] = dados.UnidadeDeMedida
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando edição de produto."));

            #endregion

            #region Validação da entrada

            var resultadoId = Id.TentarCriar(dados.Id);
            var resultadoCodigo = CodigoProduto.TentarCriar(dados.Codigo);
            var resultadoDescricao = DescricaoProduto.TentarCriar(dados.Descricao);
            var resultadoUnidade = UnidadeDeMedida.TentarCriar(dados.UnidadeDeMedida);

            var validacaoCampos = Resultado.Combinar(
                resultadoId,
                resultadoCodigo,
                resultadoDescricao,
                resultadoUnidade);

            if (validacaoCampos.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para edição de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarProdutoSaida>.Falha(validacaoCampos.Erros!);
            }

            #endregion

            #region Recuperação do agregado

            var stopwatchObterProduto = Stopwatch.StartNew();

            var resultadoProduto = await _unitOfWork.ProdutosRepository.ObterPorIdAsync(
                resultadoId.Instancia,
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
                    Mensagem: "Falha ao obter produto por id para edição.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarProdutoSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de edição de produto não encontrado.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarProdutoSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            #endregion

            #region Validação de pré-condições

            var codigoFoiAlterado = !produto.Codigo.IgualA(resultadoCodigo.Instancia);

            if (codigoFoiAlterado)
            {
                var stopwatchExisteOutroCodigo = Stopwatch.StartNew();

                var resultadoExisteCodigo = await _unitOfWork.ProdutosRepository.ExisteOutroPorCodigoAsync(
                    produto.Id,
                    resultadoCodigo.Instancia,
                    cancellationToken);

                stopwatchExisteOutroCodigo.Stop();

                _logService.RegistrarLogDebug(new RegistroDeLog(
                    Mensagem: "Verificação de duplicidade de código concluída.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["OperacaoRepositorio"] = "ExisteOutroPorCodigoAsync",
                        ["DuracaoMs"] = stopwatchExisteOutroCodigo.ElapsedMilliseconds
                    }));

                if (resultadoExisteCodigo.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao verificar duplicidade de código na edição de produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id.Valor,
                            ["Codigo"] = resultadoCodigo.Instancia.Valor,
                            ["Erros"] = resultadoExisteCodigo.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarProdutoSaida>.Falha(resultadoExisteCodigo.Erros!);
                }

                if (resultadoExisteCodigo.Instancia)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Tentativa de edição de produto com código já cadastrado para outro produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id.Valor,
                            ["Codigo"] = resultadoCodigo.Instancia.Valor,
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarProdutoSaida>.Falha("PRODUTO_JA_CADASTRADO");
                }
            }

            #endregion

            #region Execução das regras de negócio

                #region Alteração dos dados do produto

                var resultadoAlterarCodigo = produto.AlterarCodigo(resultadoCodigo.Instancia);
                var resultadoAlterarDescricao = produto.AlterarDescricao(resultadoDescricao.Instancia);
                var resultadoAlterarUnidade = produto.AlterarUnidadeDeMedida(resultadoUnidade.Instancia);

                var resultadoAlteracoes = Resultado.Combinar(
                    resultadoAlterarCodigo,
                    resultadoAlterarDescricao,
                    resultadoAlterarUnidade);

                if (resultadoAlteracoes.EhFalha)
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogError(new RegistroDeLog(
                        Mensagem: "Falha ao aplicar alterações no agregado Produto.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["ProdutoId"] = produto.Id.Valor,
                            ["Erros"] = resultadoAlteracoes.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<EditarProdutoSaida>.Falha(resultadoAlteracoes.Erros!);
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
                    Mensagem: "Falha ao atualizar produto no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoAtualizar.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarProdutoSaida>.Falha(resultadoAtualizar.Erros!);
            }

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final da edição de produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir edição de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<EditarProdutoSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto editado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id.Valor,
                    ["Ativo"] = produto.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<EditarProdutoSaida>.Sucesso(
                new EditarProdutoSaida(
                    Id: produto.Id.Valor,
                    Codigo: produto.Codigo.Valor,
                    Descricao: produto.Descricao.Valor,
                    UnidadeDeMedida: produto.UnidadeDeMedida.Valor,
                    Classificacao: produto.Classificacao.ToString(),
                    Ativo: produto.Ativo));

            #endregion
        }
    }
}
