using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.Property(e => e.RecipientEmail).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TemplateId).HasMaxLength(100);
        builder.Property(e => e.AcsMessageId).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.StatusDetail).HasMaxLength(1000);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(50);
        builder.Property(e => e.UserId).HasMaxLength(450);

        // Indexes for efficient queries
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.AcsMessageId);
        builder.HasIndex(e => new { e.OrganizationId, e.SentAt });
        builder.HasIndex(e => new { e.OrganizationId, e.Status });

        // Relationships
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
