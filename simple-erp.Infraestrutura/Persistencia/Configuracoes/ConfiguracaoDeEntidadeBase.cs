using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Core.Compartilhado.Base;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes
{  
    public abstract class ConfiguracaoDeEntidadeBase<TEntidade, TRaizDeAgregado> : IEntityTypeConfiguration<TEntidade> where TEntidade : Entidade<TRaizDeAgregado>
    {
        public void Configure(EntityTypeBuilder<TEntidade> builder)
        {
            builder.HasKey(entidade => entidade.Id);

            // O Guid é criado pela aplicação, no construtor da entidade. ValueGeneratedNever
            // impede o EF de tratar a chave como gerada pelo banco.
            builder
                .Property(entidade => entidade.Id)
                .ValueGeneratedNever();

            builder
                .Property(entidade => entidade.DataCriacaoUtc)
                .IsRequired();

            builder
                .Property(entidade => entidade.DataAtualizacaoUtc)
                .IsRequired();

            builder.Ignore(entidade => entidade.EventosDeDominio);

            ConfigurarEntidade(builder);
        }
        
        protected abstract void ConfigurarEntidade(EntityTypeBuilder<TEntidade> builder);
    }

  
    public abstract class ConfiguracaoDeEntidadeBase<TEntidade> : ConfiguracaoDeEntidadeBase<TEntidade, TEntidade> where TEntidade : Entidade<TEntidade>
    {
    }
}
