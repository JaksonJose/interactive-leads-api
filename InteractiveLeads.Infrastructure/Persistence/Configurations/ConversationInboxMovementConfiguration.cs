using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ConversationInboxMovementConfiguration : IEntityTypeConfiguration<ConversationInboxMovement>
{
    public void Configure(EntityTypeBuilder<ConversationInboxMovement> builder)
    {
        builder.ToTable("ConversationInboxMovement", "Crm");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.MovedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(512);

        builder.HasIndex(x => x.ConversationId)
            .HasDatabaseName("IX_ConversationInboxMovement_ConversationId");

        builder.HasIndex(x => x.ToInboxId)
            .HasDatabaseName("IX_ConversationInboxMovement_ToInboxId");

        builder.HasIndex(x => x.MovedAt)
            .HasDatabaseName("IX_ConversationInboxMovement_MovedAt");

        builder.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

