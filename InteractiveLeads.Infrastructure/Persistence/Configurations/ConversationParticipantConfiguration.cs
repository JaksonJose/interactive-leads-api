using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipant", "Crm");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .HasMaxLength(450);

        builder.Property(p => p.Role)
            .IsRequired();

        builder.Property(p => p.JoinedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.LeftAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.HasIndex(p => p.ConversationId)
            .HasDatabaseName("IX_ConversationParticipant_ConversationId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_ConversationParticipant_UserId");

        builder.HasIndex(p => p.ContactId)
            .HasDatabaseName("IX_ConversationParticipant_ContactId");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_ConversationParticipant_IsActive");

        builder.HasOne(p => p.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Contact)
            .WithMany()
            .HasForeignKey(p => p.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ConversationId, p.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ConversationParticipant_ConversationId_UserId_Active")
            .HasFilter("\"IsActive\" = true AND \"UserId\" IS NOT NULL");

        builder.HasIndex(p => new { p.ConversationId, p.ContactId })
            .IsUnique()
            .HasDatabaseName("IX_ConversationParticipant_ConversationId_ContactId_Active")
            .HasFilter("\"IsActive\" = true AND \"ContactId\" IS NOT NULL");
    }
}
