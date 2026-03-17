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

        builder.HasIndex(c => c.AssignedAgentId)
            .HasDatabaseName("IX_Conversation_AssignedAgentId");

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

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

