using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.ActionUrl).HasMaxLength(500);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(50);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

        // Indexes for efficient queries
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        // Relationship to ApplicationUser
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
