using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ConversationAssignmentConfiguration : IEntityTypeConfiguration<ConversationAssignment>
{
    public void Configure(EntityTypeBuilder<ConversationAssignment> builder)
    {
        builder.ToTable("ConversationAssignment", "Crm");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.AssignedBy)
            .HasMaxLength(450);

        builder.Property(a => a.AssignedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.UnassignedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.Reason)
            .HasMaxLength(512);

        builder.HasIndex(a => a.ConversationId)
            .HasDatabaseName("IX_ConversationAssignment_ConversationId");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_ConversationAssignment_UserId");

        builder.HasIndex(a => a.AssignedAt)
            .HasDatabaseName("IX_ConversationAssignment_AssignedAt");

        builder.HasOne(a => a.Conversation)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.ConversationId, a.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ConversationAssignment_ConversationId_UserId_Active")
            .HasFilter("\"UnassignedAt\" IS NULL");
    }
}
