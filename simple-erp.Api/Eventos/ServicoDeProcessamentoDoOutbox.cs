using simple_erp.Core.Compartilhado.Interfaces;

namespace simple_erp.Api.Eventos
{   
    public sealed class ServicoDeProcessamentoDoOutbox : BackgroundService
    {
        private const int TamanhoDoLotePadrao = 20;
        private const int IntervaloEmSegundosPadrao = 5;

        private readonly IProcessadorDeEventosPendentes _processador;
        private readonly ILogger<ServicoDeProcessamentoDoOutbox> _logger;
        private readonly int _tamanhoDoLote;
        private readonly TimeSpan _intervalo;

        public ServicoDeProcessamentoDoOutbox(
            IProcessadorDeEventosPendentes processador,
            IConfiguration configuracao,
            ILogger<ServicoDeProcessamentoDoOutbox> logger)
        {
            _processador = processador;
            _logger = logger;

            _tamanhoDoLote = configuracao.GetValue("Outbox:TamanhoDoLote", TamanhoDoLotePadrao);
            _intervalo = TimeSpan.FromSeconds(
                configuracao.GetValue("Outbox:IntervaloEmSegundos", IntervaloEmSegundosPadrao));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Processamento da caixa de saída iniciado (lote de {TamanhoDoLote}, intervalo de {Intervalo}s).",
                _tamanhoDoLote,
                _intervalo.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                var despachados = 0;

                try
                {
                    var resultado = await _processador.ProcessarLoteAsync(_tamanhoDoLote, stoppingToken);

                    if (resultado.EhFalha)
                        _logger.LogWarning(
                            "Falha ao processar a caixa de saída: {Erros}",
                            string.Join(" | ", resultado.Erros!));
                    else
                        despachados = resultado.Instancia;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception excecao)
                {   
                    _logger.LogError(excecao, "Erro inesperado ao processar a caixa de saída.");
                }

                // Lote cheio sugere fila acumulada — segue direto, sem esperar. O intervalo
                // existe para não martelar o banco quando não há nada a fazer.
                if (despachados >= _tamanhoDoLote)
                    continue;

                try
                {
                    await Task.Delay(_intervalo, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Processamento da caixa de saída encerrado.");
        }
    }
}
