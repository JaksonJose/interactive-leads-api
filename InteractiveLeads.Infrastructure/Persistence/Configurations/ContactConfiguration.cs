using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contact", "Crm");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Phone)
            .HasMaxLength(64);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now() at time zone 'utc'")
            .IsRequired();

        builder.HasIndex(c => c.Phone)
            .HasDatabaseName("IX_Contact_Phone");

        builder.HasIndex(c => c.CompanyId)
            .HasDatabaseName("IX_Contact_CompanyId");

        builder.HasOne(c => c.Company)
            .WithMany(co => co.Contacts)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ContactChannels)
            .WithOne(cc => cc.Contact)
            .HasForeignKey(cc => cc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

