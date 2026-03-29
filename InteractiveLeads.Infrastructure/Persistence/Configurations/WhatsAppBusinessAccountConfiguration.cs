using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class WhatsAppBusinessAccountConfiguration : IEntityTypeConfiguration<WhatsAppBusinessAccount>
{
    public void Configure(EntityTypeBuilder<WhatsAppBusinessAccount> builder)
    {
        builder.ToTable("WhatsAppBusinessAccount", "Crm");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.WabaId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(w => w.Name)
            .HasMaxLength(256);

        builder.Property(w => w.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(w => new { w.CompanyId, w.WabaId })
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppBusinessAccount_Company_Waba");

        builder.HasIndex(w => w.CompanyId)
            .HasDatabaseName("IX_WhatsAppBusinessAccount_CompanyId");

        builder.HasOne(w => w.Company)
            .WithMany(c => c.WhatsAppBusinessAccounts)
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Integration -> WABA FK is configured on IntegrationConfiguration to avoid duplicate relationship mapping.

        builder.HasMany(w => w.Templates)
            .WithOne(t => t.WhatsAppBusinessAccount)
            .HasForeignKey(t => t.WhatsAppBusinessAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
