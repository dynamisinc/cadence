using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class PendingExerciseAssignmentConfiguration : IEntityTypeConfiguration<PendingExerciseAssignment>
{
    public void Configure(EntityTypeBuilder<PendingExerciseAssignment> builder)
    {
        builder.Property(e => e.ExerciseRole).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        // Indexes for efficient queries
        builder.HasIndex(e => e.OrganizationInviteId);
        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => new { e.ExerciseId, e.Status });

        // Relationship to OrganizationInvite
        builder.HasOne(e => e.OrganizationInvite)
            .WithMany(i => i.PendingExerciseAssignments)
            .HasForeignKey(e => e.OrganizationInviteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to Exercise
        builder.HasOne(e => e.Exercise)
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.NoAction);

        // Relationship to BulkImportRecord (optional)
        builder.HasOne(e => e.BulkImportRecord)
            .WithMany(r => r.PendingAssignments)
            .HasForeignKey(e => e.BulkImportRecordId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
