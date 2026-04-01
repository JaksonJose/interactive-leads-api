using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class UserTeamConfiguration : IEntityTypeConfiguration<UserTeam>
{
    public void Configure(EntityTypeBuilder<UserTeam> builder)
    {
        builder.ToTable("UserTeam", "Crm");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.Role)
            .HasConversion<int>();

        builder.Property(m => m.JoinedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("IX_UserTeam_UserId");

        builder.HasIndex(m => m.TeamId)
            .HasDatabaseName("IX_UserTeam_TeamId");

        builder.HasIndex(m => new { m.TeamId, m.UserId })
            .IsUnique()
            .HasDatabaseName("UX_UserTeam_TeamId_UserId");

        builder.HasOne(m => m.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
