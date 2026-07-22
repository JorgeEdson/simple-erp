using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using simple_erp.Infraestrutura.Persistencia.Outbox;

namespace simple_erp.Infraestrutura.Persistencia.Configuracoes.Outbox
{   
    public sealed class EventoNoOutboxConfiguracao : IEntityTypeConfiguration<EventoNoOutbox>
    {
        public void Configure(EntityTypeBuilder<EventoNoOutbox> builder)
        {
            builder.ToTable("outbox", Esquemas.Eventos);

            builder.HasKey(evento => evento.Id);

            builder
                .Property(evento => evento.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder
                .Property(evento => evento.IdEvento)
                .HasColumnName("id_evento")
                .IsRequired();

            builder
                .Property(evento => evento.NomeDoEvento)
                .HasColumnName("nome_do_evento")
                .HasMaxLength(200)
                .IsRequired();

            builder
                .Property(evento => evento.TipoDoEvento)
                .HasColumnName("tipo_do_evento")
                .HasMaxLength(500)
                .IsRequired();

            builder
                .Property(evento => evento.IdAgregadoOrigem)
                .HasColumnName("id_agregado_origem")
                .IsRequired();

            builder
                .Property(evento => evento.Conteudo)
                .HasColumnName("conteudo")
                .HasColumnType("jsonb")
                .IsRequired();

            builder
                .Property(evento => evento.OcorridoEmUtc)
                .HasColumnName("ocorrido_em_utc")
                .IsRequired();

            builder
                .Property(evento => evento.CriadoEmUtc)
                .HasColumnName("criado_em_utc")
                .IsRequired();

            builder
                .Property(evento => evento.ProcessadoEmUtc)
                .HasColumnName("processado_em_utc");

            builder
                .Property(evento => evento.Tentativas)
                .HasColumnName("tentativas")
                .IsRequired();

            builder
                .Property(evento => evento.UltimoErro)
                .HasColumnName("ultimo_erro")
                .HasMaxLength(2000);

            // Propriedade derivada — não é coluna.
            builder.Ignore(evento => evento.EstaPendente);

            
            builder
                .HasIndex(evento => evento.CriadoEmUtc)
                .HasDatabaseName("ix_outbox_pendentes")
                .HasFilter("processado_em_utc IS NULL");

            // Consulta de auditoria: "o que aconteceu com este agregado?".
            builder
                .HasIndex(evento => new { evento.NomeDoEvento, evento.IdAgregadoOrigem })
                .HasDatabaseName("ix_outbox_evento_agregado");
        }
    }
}
