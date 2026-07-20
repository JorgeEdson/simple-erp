using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface IEmitirTituloAPagarUseCase
        : IUseCase<EmitirTituloAPagarEntrada, EmitirTituloAPagarSaida>
    {
    }

    public record EmitirTituloAPagarEntrada(
        long IdFornecedor,
        decimal Valor,
        DateTime DataVencimento,
        long? IdPedidoDeCompra = null);

    public record EmitirTituloAPagarSaida(
        long Id,
        string Tipo,
        long IdParceiro,
        decimal ValorOriginal,
        decimal SaldoDevedor,
        string Status,
        DateTime DataVencimentoUtc);

    public sealed class EmitirTituloAPagarUseCase : IEmitirTituloAPagarUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public EmitirTituloAPagarUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<EmitirTituloAPagarSaida>> ExecutarAsync(EmitirTituloAPagarEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(EmitirTituloAPagarUseCase),
                ["IdFornecedor"] = dados.IdFornecedor,
                ["Valor"] = dados.Valor
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando emissão de título a pagar."));

            #endregion

            #region Validação da entrada

            var resultadoIdFornecedor = Id.TentarCriar(dados.IdFornecedor);
            var resultadoValor = Dinheiro.TentarCriar(dados.Valor);
            var resultadoOrigem = OrigemDoTitulo.TentarCriar(TipoOrigemTitulo.Compra, dados.IdPedidoDeCompra);

            var validacao = Resultado.Combinar(resultadoIdFornecedor, resultadoValor, resultadoOrigem);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para emissão de título a pagar.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAPagarSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Validação de pré-condições

            var fornecedorExiste = await _unitOfWork.FornecedoresRepository.ExistePorIdAsync(
                resultadoIdFornecedor.Instancia, cancellationToken);

            if (fornecedorExiste.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<EmitirTituloAPagarSaida>.Falha(fornecedorExiste.Erros!);
            }

            if (!fornecedorExiste.Instancia)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de emitir título a pagar para fornecedor inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdFornecedor"] = dados.IdFornecedor,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAPagarSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Emissão do título a pagar

                var resultadoTitulo = Titulo.Criar(
                    TipoDeTitulo.APagar,
                    resultadoIdFornecedor.Instancia,
                    resultadoOrigem.Instancia,
                    resultadoValor.Instancia,
                    dados.DataVencimento);

                if (resultadoTitulo.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao criar o agregado Titulo (a pagar).",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoTitulo.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<EmitirTituloAPagarSaida>.Falha(resultadoTitulo.Erros!);
                }

                var titulo = resultadoTitulo.Instancia;

                #endregion

            #endregion

            #region Persistência

            await _unitOfWork.TitulosRepository.AdicionarAsync(titulo, cancellationToken);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogError(new RegistroDeLog(
                    Mensagem: "Falha ao persistir emissão de título a pagar.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAPagarSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Título a pagar emitido com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TituloId"] = titulo.Id.Valor,
                    ["ValorOriginal"] = titulo.ValorOriginal,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<EmitirTituloAPagarSaida>.Sucesso(
                new EmitirTituloAPagarSaida(
                    Id: titulo.Id.Valor,
                    Tipo: titulo.Tipo.ToString(),
                    IdParceiro: titulo.IdParceiro.Valor,
                    ValorOriginal: titulo.ValorOriginal,
                    SaldoDevedor: titulo.SaldoDevedor,
                    Status: titulo.Status.ToString(),
                    DataVencimentoUtc: titulo.DataVencimentoUtc));

            #endregion
        }
    }
}
