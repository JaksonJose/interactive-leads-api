using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ContactChannelConfiguration : IEntityTypeConfiguration<ContactChannel>
{
    public void Configure(EntityTypeBuilder<ContactChannel> builder)
    {
        builder.ToTable("ContactChannel", "Crm");

        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.ExternalId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(cc => cc.IsPrimary)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(cc => cc.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(cc => cc.LastSeenAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(cc => cc.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(cc => new { cc.IntegrationId, cc.ExternalId })
            .IsUnique()
            .HasDatabaseName("IX_ContactChannel_Integration_ExternalId");

        builder.HasIndex(cc => cc.ContactId)
            .HasDatabaseName("IX_ContactChannel_ContactId");

        builder.HasOne(cc => cc.Contact)
            .WithMany(c => c.ContactChannels)
            .HasForeignKey(cc => cc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cc => cc.Integration)
            .WithMany(i => i.ContactChannels)
            .HasForeignKey(cc => cc.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

