using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class CapabilityTargetConfiguration : IEntityTypeConfiguration<CapabilityTarget>
{
    public void Configure(EntityTypeBuilder<CapabilityTarget> builder)
    {
        builder.Property(e => e.TargetDescription).HasMaxLength(500).IsRequired();

        // Indexes for efficient queries
        builder.HasIndex(e => e.ExerciseId);
        builder.HasIndex(e => e.CapabilityId);
        builder.HasIndex(e => new { e.ExerciseId, e.SortOrder });

        // Relationship to Exercise (cascade delete when exercise is deleted)
        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.CapabilityTargets)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to Capability (no cascade - capability can be deactivated without deleting targets)
        builder.HasOne(e => e.Capability)
            .WithMany(c => c.CapabilityTargets)
            .HasForeignKey(e => e.CapabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to Organization (for data isolation)
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
