using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ExercisePhotoConfiguration : IEntityTypeConfiguration<ExercisePhoto>
{
    public void Configure(EntityTypeBuilder<ExercisePhoto> builder)
    {
        builder.Property(e => e.FileName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.BlobUri).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.ThumbnailUri).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.CapturedById).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PhotoStatus.Draft);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(100);
        builder.Property(e => e.AnnotationsJson).HasMaxLength(8000);

        // Indexes for efficient queries
        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => e.ObservationId);
        builder.HasIndex(e => e.CapturedById);
        builder.HasIndex(e => new { e.ExerciseId, e.CapturedAt });

        // Unique index for idempotency: one key per exercise per user (where not null)
        builder.HasIndex(e => new { e.ExerciseId, e.CapturedById, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_ExercisePhotos_Idempotency");

        // Composite index for loading photos by observation with soft-delete filter and display ordering
        builder.HasIndex(e => new { e.ObservationId, e.IsDeleted, e.DisplayOrder })
            .HasDatabaseName("IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder");

        // Relationships
        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.Photos)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Observation)
            .WithMany(o => o.Photos)
            .HasForeignKey(e => e.ObservationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Organization relationship for data isolation
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.NoAction);

        // User who captured the photo - references ApplicationUser (ASP.NET Core Identity)
        builder.HasOne(e => e.CapturedByUser)
            .WithMany()
            .HasForeignKey(e => e.CapturedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
