using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.Property(e => e.Content).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Recommendation).HasMaxLength(2000);
        builder.Property(e => e.Location).HasMaxLength(200);
        builder.Property(e => e.Rating).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(ObservationStatus.Complete);
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450);

        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => e.InjectId);
        builder.HasIndex(e => e.ObjectiveId);
        builder.HasIndex(e => e.ObservedAt);
        builder.HasIndex(e => e.CreatedByUserId);

        // Composite index for the most common query: list observations by exercise, filtered by soft delete, ordered by time
        builder.HasIndex(e => new { e.ExerciseId, e.IsDeleted, e.ObservedAt })
            .HasDatabaseName("IX_Observations_ExerciseId_IsDeleted_ObservedAt");

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.Observations)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Inject)
            .WithMany(i => i.Observations)
            .HasForeignKey(e => e.InjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Objective)
            .WithMany()
            .HasForeignKey(e => e.ObjectiveId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // User who created the observation - references ApplicationUser (ASP.NET Core Identity)
        // Uses string FK to match IdentityUser.Id type
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
