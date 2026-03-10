using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class BulkImportRowResultConfiguration : IEntityTypeConfiguration<BulkImportRowResult>
{
    public void Configure(EntityTypeBuilder<BulkImportRowResult> builder)
    {
        builder.Property(e => e.Email).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExerciseRole).HasMaxLength(50);
        builder.Property(e => e.DisplayName).HasMaxLength(200);
        builder.Property(e => e.Classification).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ErrorMessage).HasMaxLength(500);
        builder.Property(e => e.PreviousExerciseRole).HasMaxLength(50);

        // Indexes for efficient queries
        builder.HasIndex(e => e.BulkImportRecordId);
        builder.HasIndex(e => new { e.BulkImportRecordId, e.Classification });

        // Relationship to BulkImportRecord
        builder.HasOne(e => e.BulkImportRecord)
            .WithMany(r => r.RowResults)
            .HasForeignKey(e => e.BulkImportRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
