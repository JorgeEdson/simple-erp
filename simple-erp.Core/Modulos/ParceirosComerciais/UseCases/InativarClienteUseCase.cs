using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IInativarClienteUseCase : IUseCase<InativarClienteEntrada, InativarClienteSaida>
    {
    }

    public record InativarClienteEntrada(long Id);

    public record InativarClienteSaida(
       long Id,
       bool Ativo
    );

    public sealed class InativarClienteUseCase : IInativarClienteUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public InativarClienteUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<InativarClienteSaida>> ExecutarAsync(
            InativarClienteEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<InativarClienteSaida>.Falha(resultadoId.Erros!);

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoCliente.EhFalha)
                return Resultado<InativarClienteSaida>.Falha(resultadoCliente.Erros!);

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
                return Resultado<InativarClienteSaida>.Falha("CLIENTE_NAO_ENCONTRADO");

            var resultadoInativacao = cliente.Inativar();

            if (resultadoInativacao.EhFalha)
                return Resultado<InativarClienteSaida>.Falha(resultadoInativacao.Erros!);

            var resultadoAtualizar = await _unitOfWork.ClientesRepository.AtualizarAsync(
                cliente,
                cancellationToken);

            if (resultadoAtualizar.EhFalha)
                return Resultado<InativarClienteSaida>.Falha(resultadoAtualizar.Erros!);

            var resultadoSave = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (resultadoSave.EhFalha)
                return Resultado<InativarClienteSaida>.Falha(resultadoSave.Erros!);

            return Resultado<InativarClienteSaida>.Sucesso(
                new InativarClienteSaida(
                    Id: cliente.Id.Valor,
                    Ativo: cliente.Ativo));
        }
    }
}
