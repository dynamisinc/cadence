using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.ExerciseType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Location).HasMaxLength(500);

        // Clock state configuration
        builder.Property(e => e.ClockState).HasConversion<string>().HasMaxLength(20);

        // Store ClockElapsedBeforePause as bigint (ticks) to support durations > 24 hours
        builder.Property(e => e.ClockElapsedBeforePause)
            .HasConversion(
                v => v.HasValue ? v.Value.Ticks : (long?)null,
                v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

        // Timing configuration
        builder.Property(e => e.DeliveryMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TimelineMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TimeScale).HasColumnType("decimal(5,2)");

        // Exercise settings (S03-S05)
        builder.Property(e => e.ClockMultiplier).HasColumnType("decimal(4,2)").HasDefaultValue(1.0m);

        // Store MaxDuration as bigint (ticks) to support durations > 24 hours
        builder.Property(e => e.MaxDuration)
            .HasConversion(
                v => v.HasValue ? v.Value.Ticks : (long?)null,
                v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

        builder.HasIndex(e => new { e.OrganizationId, e.Status });
        builder.HasIndex(e => e.ScheduledDate);

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Exercises)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ActiveMsel)
            .WithMany()
            .HasForeignKey(e => e.ActiveMselId)
            .OnDelete(DeleteBehavior.NoAction);

        // Note: ClockStartedBy, ActivatedBy, CompletedBy, ArchivedBy store ApplicationUser IDs
        // as strings, but without FK constraints (for migration simplicity with existing data).
        // Navigation properties are ignored - user lookup should be done manually if needed.
        builder.Ignore(e => e.ClockStartedByUser);
        builder.Ignore(e => e.ActivatedByUser);
        builder.Ignore(e => e.CompletedByUser);
        builder.Ignore(e => e.ArchivedByUser);

        // Archive/delete tracking fields
        builder.Property(e => e.PreviousStatus).HasConversion<string>().HasMaxLength(20);

        // Approval workflow override fields
        builder.Property(e => e.ApprovalOverrideReason).HasMaxLength(500);
        builder.Property(e => e.ApprovalOverriddenById).HasMaxLength(450);

        // Navigation property for approval override user
        builder.HasOne(e => e.ApprovalOverriddenByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovalOverriddenById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
