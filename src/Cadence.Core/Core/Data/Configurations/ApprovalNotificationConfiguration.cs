using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ApprovalNotificationConfiguration : IEntityTypeConfiguration<ApprovalNotification>
{
    public void Configure(EntityTypeBuilder<ApprovalNotification> builder)
    {
        // String properties
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
        builder.Property(e => e.TriggeredByUserId).HasMaxLength(450);

        // Enum stored as string for readability
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);

        // Indexes for efficient queries
        builder.HasIndex(e => new { e.UserId, e.IsRead });
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });
        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => e.OrganizationId);

        // Relationships
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Inject)
            .WithMany()
            .HasForeignKey(e => e.InjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.TriggeredByUser)
            .WithMany()
            .HasForeignKey(e => e.TriggeredByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
