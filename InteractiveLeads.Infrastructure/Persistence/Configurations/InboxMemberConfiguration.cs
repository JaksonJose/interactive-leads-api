using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class InboxMemberConfiguration : IEntityTypeConfiguration<InboxMember>
{
    public void Configure(EntityTypeBuilder<InboxMember> builder)
    {
        builder.ToTable("InboxMember", "Crm");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.CanBeAssigned)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.JoinedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(m => m.InboxId)
            .HasDatabaseName("IX_InboxMember_InboxId");

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_InboxMember_UserId");

        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("IX_InboxMember_IsActive");

        builder.HasOne(m => m.Inbox)
            .WithMany(i => i.Members)
            .HasForeignKey(m => m.InboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.InboxId, m.UserId })
            .IsUnique()
            .HasDatabaseName("IX_InboxMember_InboxId_UserId_Active")
            .HasFilter("\"IsActive\" = true");
    }
}
