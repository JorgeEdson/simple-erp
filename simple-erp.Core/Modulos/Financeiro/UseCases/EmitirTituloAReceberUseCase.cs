using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.Financeiro.Entidades;
using simple_erp.Core.Modulos.Financeiro.ObjetosDeValor;
using System.Diagnostics;

namespace simple_erp.Core.Modulos.Financeiro.UseCases
{
    public interface IEmitirTituloAReceberUseCase
        : IUseCase<EmitirTituloAReceberEntrada, EmitirTituloAReceberSaida>
    {
    }

    public record EmitirTituloAReceberEntrada(
        long IdCliente,
        decimal Valor,
        DateTime DataVencimento,
        long? IdPedidoDeVenda = null);

    public record EmitirTituloAReceberSaida(
        long Id,
        string Tipo,
        long IdParceiro,
        decimal ValorOriginal,
        decimal SaldoDevedor,
        string Status,
        DateTime DataVencimentoUtc);

    public sealed class EmitirTituloAReceberUseCase : IEmitirTituloAReceberUseCase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public EmitirTituloAReceberUseCase(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<EmitirTituloAReceberSaida>> ExecutarAsync(EmitirTituloAReceberEntrada dados, CancellationToken cancellationToken = default)
        {
            #region Inicialização

            var stopwatchUseCase = Stopwatch.StartNew();

            using var escopo = _logService.IniciarEscopo(new Dictionary<string, object?>
            {
                ["CasoDeUso"] = nameof(EmitirTituloAReceberUseCase),
                ["IdCliente"] = dados.IdCliente,
                ["Valor"] = dados.Valor
            });

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Iniciando emissão de título a receber."));

            #endregion

            #region Validação da entrada

            var resultadoIdCliente = Id.TentarCriar(dados.IdCliente);
            var resultadoValor = Dinheiro.TentarCriar(dados.Valor);
            var resultadoOrigem = OrigemDoTitulo.TentarCriar(TipoOrigemTitulo.Venda, dados.IdPedidoDeVenda);

            var validacao = Resultado.Combinar(resultadoIdCliente, resultadoValor, resultadoOrigem);

            if (validacao.EhFalha)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Falha na validação dos dados para emissão de título a receber.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = validacao.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAReceberSaida>.Falha(validacao.Erros!);
            }

            #endregion

            #region Validação de pré-condições

            var clienteExiste = await _unitOfWork.ClientesRepository.ExistePorIdAsync(
                resultadoIdCliente.Instancia, cancellationToken);

            if (clienteExiste.EhFalha)
            {
                stopwatchUseCase.Stop();
                return Resultado<EmitirTituloAReceberSaida>.Falha(clienteExiste.Erros!);
            }

            if (!clienteExiste.Instancia)
            {
                stopwatchUseCase.Stop();
                _logService.RegistrarLogWarning(new RegistroDeLog(
                    Mensagem: "Tentativa de emitir título a receber para cliente inexistente.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["IdCliente"] = dados.IdCliente,
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAReceberSaida>.Falha("CLIENTE_NAO_ENCONTRADO");
            }

            #endregion

            #region Execução das regras de negócio

                #region Emissão do título a receber

                var resultadoTitulo = Titulo.Criar(
                    TipoDeTitulo.AReceber,
                    resultadoIdCliente.Instancia,
                    resultadoOrigem.Instancia,
                    resultadoValor.Instancia,
                    dados.DataVencimento);

                if (resultadoTitulo.EhFalha)
                {
                    stopwatchUseCase.Stop();
                    _logService.RegistrarLogWarning(new RegistroDeLog(
                        Mensagem: "Falha ao criar o agregado Titulo (a receber).",
                        Propriedades: new Dictionary<string, object?>
                        {
                            ["Erros"] = resultadoTitulo.Erros?.ToArray(),
                            ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                        }));
                    return Resultado<EmitirTituloAReceberSaida>.Falha(resultadoTitulo.Erros!);
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
                    Mensagem: "Falha ao persistir emissão de título a receber.",
                    Propriedades: new Dictionary<string, object?>
                    {
                        ["Erros"] = resultadoSave.Erros?.ToArray(),
                        ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                    }));
                return Resultado<EmitirTituloAReceberSaida>.Falha(resultadoSave.Erros!);
            }

            #endregion

            #region Finalização

            stopwatchUseCase.Stop();

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Título a receber emitido com sucesso.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["TituloId"] = titulo.Id.Valor,
                    ["ValorOriginal"] = titulo.ValorOriginal,
                    ["DuracaoMs"] = stopwatchUseCase.ElapsedMilliseconds
                }));

            return Resultado<EmitirTituloAReceberSaida>.Sucesso(
                new EmitirTituloAReceberSaida(
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
