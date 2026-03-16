using Finbuckle.MultiTenant;
using InteractiveLeads.Infrastructure.Identity.Models;
using InteractiveLeads.Infrastructure.Tenancy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace InteractiveLeads.Infrastructure.Context.Application
{
    public class DbConfigurations
    {
        internal class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
        {
            public void Configure(EntityTypeBuilder<ApplicationUser> builder)
            {
                builder.ToTable("Users", "Identity")
                       .IsMultiTenant();

                // TenantId configuration (nullable for global users: SysAdmin, Support)
                builder.Property(u => u.TenantId)
                       .HasMaxLength(64)
                       .IsRequired(false)
                       .HasComment("ID of the tenant to which this user belongs; NULL for global users");

                // Composite index for performance - ensures unique email per tenant
                builder.HasIndex(u => new { u.TenantId, u.Email })
                       .IsUnique(true)
                       .HasDatabaseName("IX_Users_TenantId_Email");

                // Index for fast tenant lookup
                builder.HasIndex(u => u.TenantId)
                       .HasDatabaseName("IX_Users_TenantId");

                // TenantId is just a string reference, not a FK
                // The real relationship is in the shared database (Tenants)

                // Audit fields configuration
                builder.Property(u => u.CreatedAt)
                       .ValueGeneratedOnAdd()
                       .HasColumnType("timestamp with time zone")
                       .HasDefaultValueSql("now() at time zone 'utc'")
                       .IsRequired();

                builder.Property(u => u.UpdatedAt)
                       .ValueGeneratedOnAddOrUpdate()
                       .HasColumnType("timestamp with time zone")
                       .HasDefaultValueSql("now() at time zone 'utc'")
                       .IsRequired();

                builder.Property(u => u.CreatedBy)
                       .IsRequired(false);

                builder.Property(u => u.UpdatedBy)
                       .IsRequired(false);
            }

            internal class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
            {
                public void Configure(EntityTypeBuilder<ApplicationRole> builder)
                {
                    // Roles are global (same for all tenants): do not use IsMultiTenant() to avoid duplicating roles per tenant.
                    builder.ToTable("Roles", "Identity");
                    builder.Property<string>("TenantId").IsRequired(false);

                    builder.HasIndex(r => r.NormalizedName)
                           .HasFilter("\"NormalizedName\" IS NOT NULL")
                           .IsUnique(false);

                    // Audit fields configuration
                    builder.Property(r => r.CreatedAt)
                           .ValueGeneratedOnAdd()
                           .HasColumnType("timestamp with time zone")
                           .HasDefaultValueSql("now() at time zone 'utc'")
                           .IsRequired();

                    builder.Property(r => r.UpdatedAt) 
                           .ValueGeneratedOnAddOrUpdate()
                           .HasColumnType("timestamp with time zone")
                           .HasDefaultValueSql("now() at time zone 'utc'")
                           .IsRequired();

                    builder.Property(r => r.CreatedBy)
                           .IsRequired(false);

                    builder.Property(r => r.UpdatedBy)
                           .IsRequired(false);
                }
            }

            internal class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<Guid>>
            {
                public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder)
                {
                    builder.ToTable("UserRoles", "Identity")
                           .IsMultiTenant();
                    builder.Property<string>("TenantId").IsRequired(false);
                }
            }

            internal class IdentityUserClaimsConfig : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
            {
                public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
                {
                    builder.ToTable("UserClaims", "Identity")
                           .IsMultiTenant();
                    builder.Property<string>("TenantId").IsRequired(false);
                }
            }

            internal class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
            {
                public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder)
                {
                    builder.ToTable("UserLogins", "Identity")
                           .IsMultiTenant();
                    builder.Property<string>("TenantId").IsRequired(false);
                }
            }

            internal class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<Guid>>
            {
                public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder)
                {
                    builder.ToTable("UserTokens", "Identity")
                           .IsMultiTenant();
                    builder.Property<string>("TenantId").IsRequired(false);
                }
            }

            internal class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
            {
                public void Configure(EntityTypeBuilder<RefreshToken> builder)
                {
                    builder.ToTable("RefreshTokens", "Identity")
                           .IsMultiTenant();
                    builder.Property<string>("TenantId").IsRequired(false);

                    builder.HasKey(rt => rt.Id);

                    builder.Property(rt => rt.Token)
                           .HasMaxLength(500)
                           .IsRequired();

                    builder.Property(rt => rt.DeviceInfo)
                           .HasMaxLength(200)
                           .IsRequired(false);

                    builder.Property(rt => rt.IpAddress)
                           .HasMaxLength(45);

                    builder.Property(rt => rt.CreatedAt)
                           .ValueGeneratedOnAdd()
                           .HasColumnType("timestamp with time zone")
                           .HasDefaultValueSql("now() at time zone 'utc'")
                           .IsRequired();

                    builder.Property(rt => rt.UpdatedAt)
                           .ValueGeneratedOnAddOrUpdate()
                           .HasColumnType("timestamp with time zone")
                           .HasDefaultValueSql("now() at time zone 'utc'")
                           .IsRequired();

                    builder.Property(rt => rt.CreatedBy)
                           .IsRequired(false);

                    builder.Property(rt => rt.UpdatedBy)
                           .IsRequired(false);

                    builder.Property(rt => rt.ExpirationTime)
                           .IsRequired()
                           .HasColumnType("timestamp with time zone");

                    builder.Property(rt => rt.IsRevoked)
                           .HasDefaultValue(false)
                           .IsRequired();

                    // Foreign key relationship
                    builder.HasOne(rt => rt.User)
                           .WithMany(u => u.RefreshTokens)
                           .HasForeignKey(rt => rt.UserId)
                           .OnDelete(DeleteBehavior.Cascade);

                    // Indexes for performance
                    builder.HasIndex(rt => rt.Token);

                    builder.HasIndex(rt => rt.UserId);

                    builder.HasIndex(rt => rt.ExpirationTime);
                }
            }

            internal class UserActivationTokenConfig : IEntityTypeConfiguration<UserActivationToken>
            {
                public void Configure(EntityTypeBuilder<UserActivationToken> builder)
                {
                    // Global table: no IsMultiTenant so lookup by token works without tenant context
                    builder.ToTable("ActivationTokens", "Identity");

                    builder.HasKey(t => t.Id);

                    builder.Property(t => t.Token)
                           .HasMaxLength(256)
                           .IsRequired();

                    builder.Property(t => t.ExpiresAt)
                           .HasColumnType("timestamp with time zone")
                           .IsRequired();

                    builder.Property(t => t.Used)
                           .HasDefaultValue(false)
                           .IsRequired();

                    builder.Property(t => t.CreatedAt)
                           .HasColumnType("timestamp with time zone")
                           .HasDefaultValueSql("now() at time zone 'utc'")
                           .IsRequired();

                    builder.HasOne(t => t.User)
                           .WithMany()
                           .HasForeignKey(t => t.UserId)
                           .OnDelete(DeleteBehavior.Cascade);

                    builder.HasIndex(t => t.Token).IsUnique();
                    builder.HasIndex(t => t.UserId);
                    builder.HasIndex(t => t.ExpiresAt);
                }
            }
        }
    }
}
