using FluentAssertions;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using simple_erp.Infraestrutura.Persistencia.Conversores;
using simple_erp.Testes.Compartilhado.Builders;

namespace simple_erp.Testes.Modulos.ParceirosComerciais.Repositories
{
    /// <summary>
    /// Testes unitários puros (sem banco): garantem que a ida e volta
    /// VO → primitivo → VO não perde informação — a premissa de todo o mapeamento.
    /// </summary>
    public sealed class ConversoresDeParceirosComerciaisTests
    {
        [Fact]
        public void DocumentoParaString_DeveIrEVoltarComoCpf()
        {
            var original = Documento.TentarCriar("12345678909").Instancia!;

            var paraBanco = ConversoresDeParceirosComerciais.DocumentoParaString
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeParceirosComerciais.DocumentoParaString
                .ConvertFromProviderExpression.Compile();

            var persistido = paraBanco(original);
            persistido.Should().Be("12345678909", "persiste-se o valor canônico, sem máscara");

            var rehidratado = doBanco(persistido);
            rehidratado.Valor.Should().Be(original.Valor);
            rehidratado.EhCpf.Should().BeTrue("os dígitos bastam para redescobrir o tipo");
        }

        [Fact]
        public void DocumentoParaString_DeveIrEVoltarComoCnpj()
        {
            var original = Documento.TentarCriar("11222333000181").Instancia!;

            var doBanco = ConversoresDeParceirosComerciais.DocumentoParaString
                .ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(original.Valor);
            rehidratado.EhCnpj.Should().BeTrue();
            rehidratado.Formatado.Should().Be(original.Formatado);
        }

        [Fact]
        public void EmailParaString_DeveIrEVoltarSemPerda()
        {
            var original = Email.TentarCriar("Contato@Empresa.com.BR").Instancia!;

            var paraBanco = ConversoresDeParceirosComerciais.EmailParaString
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeParceirosComerciais.EmailParaString
                .ConvertFromProviderExpression.Compile();

            var rehidratado = doBanco(paraBanco(original));
            rehidratado.Valor.Should().Be(original.Valor);
        }

        [Fact]
        public void EnderecoParaJson_DeveSerializarEDesserializarTodosOsCampos()
        {
            var original = EnderecoBuilder.Novo()
                .ComRua("Avenida Paulista")
                .ComNumero("1000")
                .ComComplemento("Conjunto 71")
                .ComBairro("Bela Vista")
                .ComCidade("São Paulo")
                .ComEstado("SP")
                .ComCep("01310-100")
                .ComPais("Brasil")
                .Criar();

            var paraBanco = ConversoresDeParceirosComerciais.EnderecoParaJson
                .ConvertToProviderExpression.Compile();
            var doBanco = ConversoresDeParceirosComerciais.EnderecoParaJson
                .ConvertFromProviderExpression.Compile();

            var json = paraBanco(original);
            json.Should().Contain("\"Cidade\"", "as chaves do json são as propriedades do record");

            var rehidratado = doBanco(json);
            rehidratado.Rua.Should().Be(original.Rua);
            rehidratado.Numero.Should().Be(original.Numero);
            rehidratado.Complemento.Should().Be(original.Complemento);
            rehidratado.Bairro.Should().Be(original.Bairro);
            rehidratado.Cidade.Should().Be(original.Cidade);
            rehidratado.Estado.Should().Be(original.Estado);
            rehidratado.Cep.Should().Be(original.Cep);
            rehidratado.Pais.Should().Be(original.Pais);
        }
    }
}
