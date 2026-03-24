using Finbuckle.MultiTenant;
using InteractiveLeads.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InteractiveLeads.Infrastructure.Persistence.Configurations;

public class MessageMediaConfiguration : IEntityTypeConfiguration<MessageMedia>
{
    public void Configure(EntityTypeBuilder<MessageMedia> builder)
    {
        builder.ToTable("MessageMedia", "Crm");

        builder.HasKey(mm => mm.Id);

        builder.Property(mm => mm.Url)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(mm => mm.MimeType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(mm => mm.Caption)
            .HasMaxLength(1024);

        builder.Property(mm => mm.FileName)
            .HasMaxLength(512);

        builder.HasOne(mm => mm.Message)
            .WithMany(m => m.Media)
            .HasForeignKey(mm => mm.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

