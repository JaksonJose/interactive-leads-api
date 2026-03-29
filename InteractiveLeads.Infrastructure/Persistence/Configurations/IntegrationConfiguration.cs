using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.ToTable("Integration", "Crm");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.ExternalIdentifier)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.Settings)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(i => i.ExternalIdentifier)
            .HasDatabaseName("IX_Integration_ExternalIdentifier");

        builder.HasIndex(i => i.CompanyId)
            .HasDatabaseName("IX_Integration_CompanyId");

        builder.HasOne(i => i.Company)
            .WithMany(c => c.Integrations)
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.WhatsAppBusinessAccountId)
            .HasDatabaseName("IX_Integration_WhatsAppBusinessAccountId");

        builder.HasOne(i => i.WhatsAppBusinessAccount)
            .WithMany(w => w.Integrations)
            .HasForeignKey(i => i.WhatsAppBusinessAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

