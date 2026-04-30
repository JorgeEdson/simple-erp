using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.CatalogoDeProdutos.Entidades;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface ICadastrarProdutoUseCase
        : IUseCase<CadastrarProdutoEntrada, CadastrarProdutoSaida>
    {
    }

    public record CadastrarProdutoEntrada(
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string? Classificacao = null
    );

    public record CadastrarProdutoSaida(
        long Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string Classificacao,
        bool Ativo
    );

    public sealed class CadastrarProdutoUseCase : ICadastrarProdutoUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public CadastrarProdutoUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<CadastrarProdutoSaida>> ExecutarAsync(
            CadastrarProdutoEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(CadastrarProdutoUseCase),
                ["Codigo"] = dados.Codigo,
                ["Descricao"] = dados.Descricao,
                ["UnidadeDeMedida"] = dados.UnidadeDeMedida
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando cadastro de produto."));

            var resultadoCodigo = CodigoProduto.TentarCriar(dados.Codigo);
            var resultadoDescricao = DescricaoProduto.TentarCriar(dados.Descricao);
            var resultadoUnidade = UnidadeDeMedida.TentarCriar(dados.UnidadeDeMedida);

            var validacaoCampos = Resultado.Combinar(
                resultadoCodigo,
                resultadoDescricao,
                resultadoUnidade);

            if (validacaoCampos.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para cadastro de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacaoCampos.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarProdutoSaida>.Falha(validacaoCampos.Erros!);
            }

            var classificacao = ClassificacaoProduto.SemClassificacao;

            if (!string.IsNullOrWhiteSpace(dados.Classificacao))
            {
                if (!Enum.TryParse<ClassificacaoProduto>(dados.Classificacao, true, out classificacao))
                {
                    stopwatchUseCase.Stop();

                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Classificação informada para cadastro de produto é inválida.",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Classificacao"] = dados.Classificacao,
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));

                    return Resultado<CadastrarProdutoSaida>.Falha("CLASSIFICACAO_PRODUTO_INVALIDA");
                }
            }

            var stopwatchExisteCodigo = Stopwatch.StartNew();

            var jaExiste = await _unitOfWork.ProdutosRepository.ExistePorCodigoAsync(
                resultadoCodigo.Instancia,
                cancellationToken);

            stopwatchExisteCodigo.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Verificação de existência de produto por código concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "ExistePorCodigoAsync",
                    ["DuracaoMs"] = stopwatchExisteCodigo.ElapsedMilliseconds
                }));

            if (jaExiste.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao verificar existência de produto por código.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Codigo"] = resultadoCodigo.Instancia.Valor,
                        ["Erros"] = jaExiste.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarProdutoSaida>.Falha(jaExiste.Erros!);
            }

            if (jaExiste.Instancia)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de cadastro de produto com código já existente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Codigo"] = resultadoCodigo.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarProdutoSaida>.Falha("PRODUTO_JA_CADASTRADO");
            }

            var stopwatchCriarAgregado = Stopwatch.StartNew();

            var resultadoProduto = Produto.Criar(
                resultadoCodigo.Instancia,
                resultadoDescricao.Instancia,
                resultadoUnidade.Instancia,
                classificacao);

            stopwatchCriarAgregado.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Criação do agregado Produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoDominio"] = "Produto.Criar",
                    ["DuracaoMs"] = stopwatchCriarAgregado.ElapsedMilliseconds
                }));

            if (resultadoProduto.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao criar agregado Produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Codigo"] = resultadoCodigo.Instancia.Valor,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarProdutoSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            var stopwatchAdicionar = Stopwatch.StartNew();

            await _unitOfWork.ProdutosRepository.AdicionarAsync(
                produto,
                cancellationToken);

            stopwatchAdicionar.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Adição de produto no repositório concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "AdicionarAsync",
                    ["DuracaoMs"] = stopwatchAdicionar.ElapsedMilliseconds
                }));

            var stopwatchSaveChanges = Stopwatch.StartNew();

            var resultadoSaveChanges = await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatchSaveChanges.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Persistência final do cadastro de produto concluída.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoRepositorio"] = "SaveChangesAsync",
                    ["DuracaoMs"] = stopwatchSaveChanges.ElapsedMilliseconds
                }));

            if (resultadoSaveChanges.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir cadastro de produto.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = produto.Id.Valor,
                        ["Codigo"] = produto.Codigo.Valor,
                        ["Erros"] = resultadoSaveChanges.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<CadastrarProdutoSaida>.Falha(resultadoSaveChanges.Erros!);
            }

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto cadastrado com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id.Valor,
                    ["Ativo"] = produto.Ativo,
                    ["Classificacao"] = produto.Classificacao.ToString(),
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<CadastrarProdutoSaida>.Sucesso(
                new CadastrarProdutoSaida(
                    Id: produto.Id.Valor,
                    Codigo: produto.Codigo.Valor,
                    Descricao: produto.Descricao.Valor,
                    UnidadeDeMedida: produto.UnidadeDeMedida.Valor,
                    Classificacao: produto.Classificacao.ToString(),
                    Ativo: produto.Ativo));
        }
    }
}
