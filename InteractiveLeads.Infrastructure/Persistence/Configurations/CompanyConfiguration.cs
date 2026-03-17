using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Company", "Crm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Document)
            .HasMaxLength(32);

        builder.Property(c => c.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("IX_Company_TenantId");

        builder.HasOne(c => c.Tenant)
            .WithMany(t => t.Companies)
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Integrations)
            .WithOne(i => i.Company)
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Contacts)
            .WithOne(ct => ct.Company)
            .HasForeignKey(ct => ct.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Conversations)
            .WithOne(cv => cv.Company)
            .HasForeignKey(cv => cv.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

