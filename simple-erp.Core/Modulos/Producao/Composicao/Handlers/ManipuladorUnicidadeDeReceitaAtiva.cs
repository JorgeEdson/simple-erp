using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Modulos.Producao.Composicao.Eventos;

namespace simple_erp.Core.Modulos.Producao.Composicao.Handlers
{
    /// <summary>
    /// Handler de Domain Event INTRA-CONTEXTO (produtor e consumidor no mesmo bounded
    /// context): quando uma versão de receita é ativada, desativa a versão anteriormente
    /// ativa do mesmo produto, garantindo a invariante "apenas uma receita ativa por
    /// produto". Contraponto didático aos handlers de integração entre contextos.
    /// </summary>
    public sealed class ManipuladorUnicidadeDeReceitaAtiva
        : IManipuladorDeEventoDeDominio<ComposicaoDeProdutoAtivada>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogService _logService;

        public ManipuladorUnicidadeDeReceitaAtiva(
            IUnitOfWork unitOfWork,
            ILogService logService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
        }

        public async Task<Resultado<bool>> ManipularAsync(
            ComposicaoDeProdutoAtivada evento,
            CancellationToken cancellationToken = default)
        {
            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Reagindo à ativação de composição: garantindo unicidade da receita ativa.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["Handler"] = nameof(ManipuladorUnicidadeDeReceitaAtiva),
                    ["ComposicaoId"] = evento.IdComposicao.Valor,
                    ["IdProdutoFabricado"] = evento.IdProdutoFabricado.Valor,
                    ["Versao"] = evento.Versao
                }));

            var existeAtiva = await _unitOfWork.ComposicoesDeProdutoRepository
                .ExisteAtivaPorProdutoAsync(evento.IdProdutoFabricado, cancellationToken);

            if (existeAtiva.EhFalha)
                return Resultado<bool>.Falha(existeAtiva.Erros!);

            if (!existeAtiva.Instancia)
                return Resultado<bool>.Sucesso(true);

            var resultadoAtiva = await _unitOfWork.ComposicoesDeProdutoRepository
                .ObterAtivaPorProdutoAsync(evento.IdProdutoFabricado, cancellationToken);

            if (resultadoAtiva.EhFalha)
                return Resultado<bool>.Falha(resultadoAtiva.Erros!);

            var ativaAtual = resultadoAtiva.Instancia;

            // Nada a fazer se a única ativa já é a própria versão recém-ativada.
            if (ativaAtual is null || ativaAtual.Id.IgualA(evento.IdComposicao))
                return Resultado<bool>.Sucesso(true);

            ativaAtual.Inativar();

            var resultadoAtualizar = await _unitOfWork.ComposicoesDeProdutoRepository
                .AtualizarAsync(ativaAtual, cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<bool>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<bool>.Falha(resultadoSave.Erros!);

            _logService.RegistrarLogInformation(new RegistroDeLog(
                Mensagem: "Versão anteriormente ativa da receita foi desativada.",
                Propriedades: new Dictionary<string, object?>
                {
                    ["ComposicaoDesativadaId"] = ativaAtual.Id.Valor,
                    ["VersaoDesativada"] = ativaAtual.Versao
                }));

            return Resultado<bool>.Sucesso(true);
        }
    }
}
