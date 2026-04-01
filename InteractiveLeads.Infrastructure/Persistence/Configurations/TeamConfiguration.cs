using InteractiveLeads.Domain.Entities;
using InteractiveLeads.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Team", "Crm");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.AutoAssignEnabled)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.AutoAssignStrategy)
            .HasConversion<int>()
            .HasDefaultValue(AutoAssignStrategy.RoundRobin)
            .IsRequired();

        builder.Property(t => t.AutoAssignIgnoreOfflineUsers)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(t => t.AutoAssignMaxConversationsPerUser);

        builder.Property(t => t.AutoAssignReassignTimeoutMinutes);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(Team.MaxNameLength);

        builder.Property(t => t.Description)
            .HasMaxLength(Team.MaxDescriptionLength);

        builder.Property(t => t.IsActive)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(t => t.TenantId)
            .HasDatabaseName("IX_Team_TenantId");

        builder.HasIndex(t => t.CompanyId)
            .HasDatabaseName("IX_Team_CompanyId");

        builder.HasOne(t => t.Tenant)
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Company)
            .WithMany(c => c.Teams)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
