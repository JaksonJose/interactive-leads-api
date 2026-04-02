using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversation", "Crm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.AssignedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.Priority)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.LastMessage)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(c => c.LastMessageAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(c => c.CompanyId)
            .HasDatabaseName("IX_Conversation_CompanyId");

        builder.HasIndex(c => c.ContactId)
            .HasDatabaseName("IX_Conversation_ContactId");

        builder.HasIndex(c => c.IntegrationId)
            .HasDatabaseName("IX_Conversation_IntegrationId");

        builder.HasIndex(c => c.InboxId)
            .HasDatabaseName("IX_Conversation_InboxId");

        builder.HasIndex(c => new { c.InboxId, c.Status, c.AssignedAgentId })
            .HasDatabaseName("IX_Conversation_InboxId_Status_AssignedAgentId");

        builder.HasIndex(c => c.AssignedAgentId)
            .HasDatabaseName("IX_Conversation_AssignedAgentId");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Conversation_Status");

        builder.HasIndex(c => c.Priority)
            .HasDatabaseName("IX_Conversation_Priority");

        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_Conversation_LastMessageAt");

        builder.HasOne(c => c.Company)
            .WithMany(co => co.Conversations)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Contact)
            .WithMany(ct => ct.Conversations)
            .HasForeignKey(c => c.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Integration)
            .WithMany(i => i.Conversations)
            .HasForeignKey(c => c.IntegrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Inbox)
            .WithMany(i => i.Conversations)
            .HasForeignKey(c => c.InboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.HandlingTeam)
            .WithMany(t => t.HandledConversations)
            .HasForeignKey(c => c.HandlingTeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.EffectiveSlaPolicyId);
        builder.Property(c => c.FirstResponseDueAt).HasColumnType("timestamp with time zone");
        builder.Property(c => c.ResolutionDueAt).HasColumnType("timestamp with time zone");
        builder.Property(c => c.FirstAgentResponseAt).HasColumnType("timestamp with time zone");

        builder.HasOne(c => c.EffectiveSlaPolicy)
            .WithMany()
            .HasForeignKey(c => c.EffectiveSlaPolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.HandlingTeamId)
            .HasDatabaseName("IX_Conversation_HandlingTeamId");

        builder.HasIndex(c => c.EffectiveSlaPolicyId)
            .HasDatabaseName("IX_Conversation_EffectiveSlaPolicyId");

        builder.HasIndex(c => c.FirstResponseDueAt)
            .HasDatabaseName("IX_Conversation_FirstResponseDueAt");

        builder.HasMany(c => c.Assignments)
            .WithOne(a => a.Conversation)
            .HasForeignKey(a => a.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

