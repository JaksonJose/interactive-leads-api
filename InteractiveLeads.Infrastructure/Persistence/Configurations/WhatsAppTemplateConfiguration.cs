using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class WhatsAppTemplateConfiguration : IEntityTypeConfiguration<WhatsAppTemplate>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplate> builder)
    {
        builder.ToTable("WhatsAppTemplate", "Crm");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.MetaTemplateId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(t => t.Language)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.SubmissionLastError)
            .HasMaxLength(2000);

        builder.Property(t => t.SubmissionLastErrorCode)
            .HasMaxLength(128);

        builder.Property(t => t.SubmissionLastErrorAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.ComponentsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(t => t.LastSyncedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(t => new { t.WhatsAppBusinessAccountId, t.Name, t.Language })
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppTemplate_Waba_Name_Language");

        builder.HasIndex(t => t.WhatsAppBusinessAccountId)
            .HasDatabaseName("IX_WhatsAppTemplate_WhatsAppBusinessAccountId");
    }
}
