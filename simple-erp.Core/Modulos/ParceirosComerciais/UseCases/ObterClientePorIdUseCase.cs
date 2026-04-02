using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IObterClientePorIdUseCase : IUseCase<ObterClientePorIdEntrada, ObterClientePorIdSaida>
    {
    }

    public sealed record ObterClientePorIdEntrada(long Id);

    public sealed record ObterClientePorIdSaida(
        long Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc,
        ObterClientePorIdEnderecoSaida Endereco);

    public sealed record ObterClientePorIdEnderecoSaida(
        string Rua,
        string Numero,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string? Complemento,
        string Pais);
    public sealed class ObterClientePorIdUseCase : IObterClientePorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ObterClientePorIdUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<ObterClientePorIdSaida>> ExecutarAsync(
            ObterClientePorIdEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<ObterClientePorIdSaida>.Falha(resultadoId.Erros!);

            var resultadoCliente = await _unitOfWork.ClientesRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoCliente.EhFalha)
                return Resultado<ObterClientePorIdSaida>.Falha(resultadoCliente.Erros!);

            var cliente = resultadoCliente.Instancia;

            if (cliente is null)
                return Resultado<ObterClientePorIdSaida>.Falha("CLIENTE_NAO_ENCONTRADO");

            return Resultado<ObterClientePorIdSaida>.Sucesso(
                new ObterClientePorIdSaida(
                    Id: cliente.Id.Valor,
                    Nome: cliente.Nome.Valor,
                    Documento: cliente.Documento.Valor,
                    Email: cliente.Email.Valor,
                    Ativo: cliente.Ativo,
                    DataCriacaoUtc: cliente.DataCriacaoUtc,
                    DataAtualizacaoUtc: cliente.DataAtualizacaoUtc,
                    Endereco: new ObterClientePorIdEnderecoSaida(
                        Rua: cliente.Endereco.Rua,
                        Numero: cliente.Endereco.Numero,
                        Bairro: cliente.Endereco.Bairro,
                        Cidade: cliente.Endereco.Cidade,
                        Estado: cliente.Endereco.Estado,
                        Cep: cliente.Endereco.Cep,
                        Complemento: cliente.Endereco.Complemento,
                        Pais: cliente.Endereco.Pais)));
        }
    }
}
