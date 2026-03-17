using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("MessageReaction", "Crm");

        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.Reaction)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(mr => mr.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasOne(mr => mr.Message)
            .WithMany(m => m.Reactions)
            .HasForeignKey(mr => mr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

