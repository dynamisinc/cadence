using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class InjectObjectiveConfiguration : IEntityTypeConfiguration<InjectObjective>
{
    public void Configure(EntityTypeBuilder<InjectObjective> builder)
    {
        // Composite primary key
        builder.HasKey(e => new { e.InjectId, e.ObjectiveId });

        // Matching query filter for required Inject navigation (Inject has soft-delete filter)
        builder.HasQueryFilter(e => !e.Inject.IsDeleted);

        // Indexes for efficient queries
        builder.HasIndex(e => e.InjectId);
        builder.HasIndex(e => e.ObjectiveId);

        // Relationships
        builder.HasOne(e => e.Inject)
            .WithMany(i => i.InjectObjectives)
            .HasForeignKey(e => e.InjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Objective)
            .WithMany(o => o.InjectObjectives)
            .HasForeignKey(e => e.ObjectiveId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
