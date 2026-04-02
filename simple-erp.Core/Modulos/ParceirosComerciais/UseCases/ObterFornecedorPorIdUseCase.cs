using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.Interfaces;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using System;
using System.Collections.Generic;
using System.Text;

namespace simple_erp.Core.Modulos.ParceirosComerciais.UseCases
{
    public interface IObterFornecedorPorIdUseCase
       : IUseCase<ObterFornecedorPorIdEntrada, ObterFornecedorPorIdSaida>
    {
    }

    public record ObterFornecedorPorIdEntrada(long Id);

    public record ObterFornecedorPorIdSaida(
        long Id,
        string Nome,
        string Documento,
        string Email,
        bool Ativo,
        DateTime DataCriacaoUtc,
        DateTime? DataAtualizacaoUtc,
        ObterFornecedorPorIdEnderecoSaida Endereco
    );

    public record ObterFornecedorPorIdEnderecoSaida(
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string Pais
    );
    public sealed class ObterFornecedorPorIdUseCase : IObterFornecedorPorIdUseCase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ObterFornecedorPorIdUseCase(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Resultado<ObterFornecedorPorIdSaida>> ExecutarAsync(
            ObterFornecedorPorIdEntrada dados,
            CancellationToken cancellationToken = default)
        {
            var resultadoId = Id.TentarCriar(dados.Id);

            if (resultadoId.EhFalha)
                return Resultado<ObterFornecedorPorIdSaida>.Falha(resultadoId.Erros!);

            var resultadoFornecedor = await _unitOfWork.FornecedoresRepository.ObterPorIdAsync(
                resultadoId.Instancia,
                cancellationToken);

            if (resultadoFornecedor.EhFalha)
                return Resultado<ObterFornecedorPorIdSaida>.Falha(resultadoFornecedor.Erros!);

            var fornecedor = resultadoFornecedor.Instancia;

            if (fornecedor is null)
                return Resultado<ObterFornecedorPorIdSaida>.Falha("FORNECEDOR_NAO_ENCONTRADO");

            return Resultado<ObterFornecedorPorIdSaida>.Sucesso(
                new ObterFornecedorPorIdSaida(
                    Id: fornecedor.Id.Valor,
                    Nome: fornecedor.Nome.Valor,
                    Documento: fornecedor.Documento.Valor,
                    Email: fornecedor.Email.Valor,
                    Ativo: fornecedor.Ativo,
                    DataCriacaoUtc: fornecedor.DataCriacaoUtc,
                    DataAtualizacaoUtc: fornecedor.DataAtualizacaoUtc,
                    Endereco: new ObterFornecedorPorIdEnderecoSaida(
                        Rua: fornecedor.Endereco.Rua,
                        Numero: fornecedor.Endereco.Numero,
                        Complemento: fornecedor.Endereco.Complemento,
                        Bairro: fornecedor.Endereco.Bairro,
                        Cidade: fornecedor.Endereco.Cidade,
                        Estado: fornecedor.Endereco.Estado,
                        Cep: fornecedor.Endereco.Cep,
                        Pais: fornecedor.Endereco.Pais)));
        }
    }
}
