using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class InboxTeamConfiguration : IEntityTypeConfiguration<InboxTeam>
{
    public void Configure(EntityTypeBuilder<InboxTeam> builder)
    {
        builder.ToTable("InboxTeam", "Crm");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.InboxId)
            .HasDatabaseName("IX_InboxTeam_InboxId");

        builder.HasIndex(x => x.TeamId)
            .HasDatabaseName("IX_InboxTeam_TeamId");

        builder.HasIndex(x => new { x.InboxId, x.TeamId })
            .IsUnique()
            .HasDatabaseName("UX_InboxTeam_InboxId_TeamId");

        builder.HasOne(x => x.Inbox)
            .WithMany(i => i.TeamLinks)
            .HasForeignKey(x => x.InboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany(t => t.InboxLinks)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
