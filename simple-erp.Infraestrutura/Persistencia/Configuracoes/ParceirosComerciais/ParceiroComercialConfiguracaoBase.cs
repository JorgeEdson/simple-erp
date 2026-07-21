using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Modulos.ParceirosComerciais.Entidades;
using simple_erp.Infraestrutura.Persistencia.Conversores;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.ParceirosComerciais
{   
    public abstract class ParceiroComercialConfiguracaoBase<TParceiro> : ConfiguracaoDeEntidadeBase<TParceiro, ParceiroComercial> where TParceiro : ParceiroComercial
    {
        protected abstract string NomeDaTabela { get; }

        protected override void ConfigurarEntidade(EntityTypeBuilder<TParceiro> builder)
        {
            builder.HasBaseType((Type?)null);

            builder.ToTable(NomeDaTabela, Esquemas.ParceirosComerciais);

            builder
                .Property(parceiro => parceiro.Nome)
                .HasConversion(ConversoresDeObjetosDeValor.NomeParaString)
                .HasColumnName("nome")
                .HasMaxLength(100)
                .IsRequired();

            builder
                .Property(parceiro => parceiro.Documento)
                .HasConversion(ConversoresDeParceirosComerciais.DocumentoParaString)
                .HasColumnName("documento")
                .HasMaxLength(20)
                .IsRequired();

            builder
                .Property(parceiro => parceiro.Email)
                .HasConversion(ConversoresDeParceirosComerciais.EmailParaString)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();

            builder
                .Property(parceiro => parceiro.Endereco)
                .HasConversion(ConversoresDeParceirosComerciais.EnderecoParaJson)
                .HasColumnName("endereco")
                .HasColumnType("jsonb")
                .IsRequired();

            builder
                .Property(parceiro => parceiro.Ativo)
                .HasColumnName("ativo")
                .IsRequired();

            // Unicidade de documento por tipo de parceiro (requisito dos cadastros).
            builder
                .HasIndex(parceiro => parceiro.Documento)
                .IsUnique()
                .HasDatabaseName($"ux_{NomeDaTabela}_documento");
        }
    }

    public sealed class ClienteConfiguracao : ParceiroComercialConfiguracaoBase<Cliente>
    {
        protected override string NomeDaTabela => "clientes";
    }

    public sealed class FornecedorConfiguracao : ParceiroComercialConfiguracaoBase<Fornecedor>
    {
        protected override string NomeDaTabela => "fornecedores";
    }
}
