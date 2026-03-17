using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Message", "Crm");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ExternalMessageId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(m => m.Metadata)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(m => m.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(m => m.ConversationId)
            .HasDatabaseName("IX_Message_ConversationId");

        builder.HasIndex(m => m.SenderUserId)
            .HasDatabaseName("IX_Message_SenderUserId");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ReplyToMessage)
            .WithMany()
            .HasForeignKey(m => m.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

