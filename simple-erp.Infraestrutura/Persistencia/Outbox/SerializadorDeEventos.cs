using simple_erp.Core.Compartilhado.Base;
using simple_erp.Core.Compartilhado.ObjetosDeValor;
using simple_erp.Core.Modulos.CatalogoDeProdutos.ObjetosDeValor;
using simple_erp.Core.Modulos.ParceirosComerciais.ObjetosDeValor;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace simple_erp.Infraestrutura.Persistencia.Outbox
{
    /// <summary>
    /// Serializa e reidrata eventos de domínio para o outbox.
    ///
    /// Dois detalhes tornam o round-trip possível:
    ///
    /// 1) Os Value Objects têm construtor privado e fábrica TentarCriar, então o
    ///    serializador não conseguiria reconstruí-los sozinho. Os conversores abaixo os
    ///    tratam como o valor primitivo que eles encapsulam — que é, aliás, como devem
    ///    aparecer no JSON: <c>"documento": "12345678909"</c> e não um objeto aninhado.
    ///
    /// 2) Os eventos não têm construtor sem parâmetros nem setters — mas têm um único
    ///    construtor público cujos parâmetros casam com os nomes das propriedades. O
    ///    System.Text.Json usa esse construtor na desserialização.
    ///
    /// Ressalva consciente: <c>IdEvento</c> e <c>DataOcorrenciaUtc</c> são gerados pelo
    /// construtor base e não são parâmetros dos construtores derivados; ao reidratar, o
    /// objeto ganha valores novos para esses dois campos. Os valores originais ficam
    /// preservados nas COLUNAS do outbox, que é o registro de verdade para auditoria.
    /// </summary>
    public static class SerializadorDeEventos
    {
        private static readonly JsonSerializerOptions Opcoes = CriarOpcoes();

        public static string Serializar(EventoDeDominio evento) =>
            // O tipo em runtime é essencial: serializar como EventoDeDominio perderia
            // todas as propriedades do evento concreto.
            JsonSerializer.Serialize(evento, evento.GetType(), Opcoes);

        public static EventoDeDominio? Desserializar(string tipoDoEvento, string conteudo)
        {
            // Todos os eventos vivem no assembly do Core; resolver por lá evita depender
            // do nome/versão do assembly gravado no banco.
            var tipo = typeof(EventoDeDominio).Assembly.GetType(tipoDoEvento);

            if (tipo is null)
                return null;

            return JsonSerializer.Deserialize(conteudo, tipo, Opcoes) as EventoDeDominio;
        }

        private static JsonSerializerOptions CriarOpcoes()
        {
            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            opcoes.Converters.Add(new ConversorJsonDeId());

            // Todo VO que aparece em algum construtor de evento precisa de conversor.
            // Se um evento novo passar a carregar outro VO, registre-o aqui.
            opcoes.Converters.Add(new ConversorDeVoDeTexto<Nome>(
                texto => Nome.TentarCriar(texto, null).Instancia!, vo => vo.Valor));

            opcoes.Converters.Add(new ConversorDeVoDeTexto<Documento>(
                texto => Documento.TentarCriar(texto, null).Instancia!, vo => vo.Valor));

            opcoes.Converters.Add(new ConversorDeVoDeTexto<CodigoProduto>(
                texto => CodigoProduto.TentarCriar(texto, null).Instancia!, vo => vo.Valor));

            opcoes.Converters.Add(new ConversorDeVoDeTexto<DescricaoProduto>(
                texto => DescricaoProduto.TentarCriar(texto, null).Instancia!, vo => vo.Valor));

            return opcoes;
        }

        private sealed class ConversorJsonDeId : JsonConverter<Id>
        {
            public override Id Read(
                ref Utf8JsonReader leitor, Type tipoAConverter, JsonSerializerOptions opcoes) =>
                Id.TentarCriar(leitor.GetInt64()).Instancia!;

            public override void Write(
                Utf8JsonWriter escritor, Id valor, JsonSerializerOptions opcoes) =>
                escritor.WriteNumberValue(valor.Valor);
        }

        /// <summary>
        /// Conversor genérico para Value Objects que encapsulam um texto. A criação e a
        /// leitura do valor entram como delegates porque cada VO tem sua própria fábrica
        /// TentarCriar — o que também significa que a validação do domínio volta a ser
        /// aplicada quando o evento é reidratado.
        /// </summary>
        private sealed class ConversorDeVoDeTexto<TValueObject> : JsonConverter<TValueObject>
            where TValueObject : class
        {
            private readonly Func<string, TValueObject> _criar;
            private readonly Func<TValueObject, string> _lerValor;

            public ConversorDeVoDeTexto(
                Func<string, TValueObject> criar,
                Func<TValueObject, string> lerValor)
            {
                _criar = criar;
                _lerValor = lerValor;
            }

            public override TValueObject Read(
                ref Utf8JsonReader leitor, Type tipoAConverter, JsonSerializerOptions opcoes) =>
                _criar(leitor.GetString() ?? string.Empty);

            public override void Write(
                Utf8JsonWriter escritor, TValueObject valor, JsonSerializerOptions opcoes) =>
                escritor.WriteStringValue(_lerValor(valor));
        }
    }
}
