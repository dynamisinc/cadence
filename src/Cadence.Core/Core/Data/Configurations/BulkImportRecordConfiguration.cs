using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class BulkImportRecordConfiguration : IEntityTypeConfiguration<BulkImportRecord>
{
    public void Configure(EntityTypeBuilder<BulkImportRecord> builder)
    {
        builder.Property(e => e.ImportedById).HasMaxLength(450).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(255).IsRequired();

        // Indexes for efficient queries
        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => new { e.ExerciseId, e.ImportedAt });

        // Relationship to Exercise
        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to ImportedBy user
        builder.HasOne(e => e.ImportedBy)
            .WithMany()
            .HasForeignKey(e => e.ImportedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
