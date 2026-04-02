using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicy", "Crm");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(SlaPolicy.MaxCodeLength);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(SlaPolicy.MaxNameLength);

        builder.Property(p => p.Description)
            .HasMaxLength(SlaPolicy.MaxDescriptionLength);

        builder.Property(p => p.FirstResponseTargetMinutes)
            .IsRequired();

        builder.Property(p => p.ResolutionTargetMinutes)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_SlaPolicy_TenantId");

        builder.HasIndex(p => p.CompanyId)
            .HasDatabaseName("IX_SlaPolicy_CompanyId");

        builder.HasIndex(p => new { p.CompanyId, p.IsActive })
            .HasDatabaseName("IX_SlaPolicy_CompanyId_IsActive");

        builder.HasIndex(p => new { p.CompanyId, p.CreatedAt })
            .HasDatabaseName("IX_SlaPolicy_CompanyId_CreatedAt");

        builder.HasIndex(p => new { p.CompanyId, p.UpdatedAt })
            .HasDatabaseName("IX_SlaPolicy_CompanyId_UpdatedAt");

        builder.HasIndex(p => new { p.CompanyId, p.Code })
            .IsUnique()
            .HasDatabaseName("IX_SlaPolicy_CompanyId_Code")
            .HasFilter("\"Code\" IS NOT NULL");

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Company)
            .WithMany(c => c.SlaPolicies)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
