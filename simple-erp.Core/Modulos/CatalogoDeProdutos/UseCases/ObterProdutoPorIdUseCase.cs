using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace simple_erp.Core.Modulos.CatalogoDeProdutos.UseCases
{
    public interface IObterProdutoPorIdUseCase : IUseCase<ObterProdutoPorIdEntrada, ObterProdutoPorIdSaida>
    {
    }

    public sealed record ObterProdutoPorIdEntrada(long Id);

    public sealed record ObterProdutoPorIdSaida(
        long Id,
        string Codigo,
        string Descricao,
        string UnidadeDeMedida,
        string Classificacao,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc);

    public sealed class ObterProdutoPorIdUseCase : IObterProdutoPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ObterProdutoPorIdUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<ObterProdutoPorIdSaida>> ExecutarAsync(
            ObterProdutoPorIdEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(ObterProdutoPorIdUseCase),
                ["ProdutoId"] = dados.Id
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando obtenção de produto por id."));

            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação do identificador para obtenção de produto por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = dados.Id,
                        ["Erros"] = resultadoId.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterProdutoPorIdSaida>.Falha(resultadoId.Erros!);
            }

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
                    Mensagem: "Falha ao obter produto por id no repositório.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["Erros"] = resultadoProduto.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterProdutoPorIdSaida>.Falha(resultadoProduto.Erros!);
            }

            var produto = resultadoProduto.Instancia;

            if (produto is null)
            {
                stopwatchUseCase.Stop();

                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de obtenção de produto não encontrado por id.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["ProdutoId"] = resultadoId.Instancia.Valor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));

                return Resultado<ObterProdutoPorIdSaida>.Falha("PRODUTO_NAO_ENCONTRADO");
            }

            var stopwatchMapeamento = Stopwatch.StartNew();

            var saida = new ObterProdutoPorIdSaida(
                Id: produto.Id.Valor,
                Codigo: produto.Codigo.Valor,
                Descricao: produto.Descricao.Valor,
                UnidadeDeMedida: produto.UnidadeDeMedida.Valor,
                Classificacao: produto.Classificacao.ToString(),
                Ativo: produto.Ativo,
                DataCriacaoUtc: produto.DataCriacaoUtc,
                DataAtualizacaoUtc: produto.DataAtualizacaoUtc);

            stopwatchMapeamento.Stop();

            _logService.RegistrarLogDebug(new RegistroDeLog(
                Mensagem: "Mapeamento da saída de obtenção de produto por id concluído.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["OperacaoMapeamento"] = "ObterProdutoPorIdSaida",
                    ["DuracaoMs"] = stopwatchMapeamento.ElapsedMilliseconds
                }));

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Produto obtido por id com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ProdutoId"] = produto.Id.Valor,
                    ["Ativo"] = produto.Ativo,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<ObterProdutoPorIdSaida>.Sucesso(saida);
        }
    }
}
