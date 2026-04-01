using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class InboxConfiguration : IEntityTypeConfiguration<Inbox>
{
    public void Configure(EntityTypeBuilder<Inbox> builder)
    {
        builder.ToTable("Inbox", "Crm");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.IsActive)
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(i => i.CompanyId)
            .HasDatabaseName("IX_Inbox_CompanyId");

        builder.HasIndex(i => i.IsActive)
            .HasDatabaseName("IX_Inbox_IsActive");

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Conversations)
            .WithOne(c => c.Inbox)
            .HasForeignKey(c => c.InboxId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
