using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class EegEntryConfiguration : IEntityTypeConfiguration<EegEntry>
{
    public void Configure(EntityTypeBuilder<EegEntry> builder)
    {
        builder.Property(e => e.ObservationText).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Rating).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.EvaluatorId).HasMaxLength(450).IsRequired();

        // Indexes for efficient queries
        builder.HasIndex(e => e.CriticalTaskId);
        builder.HasIndex(e => e.EvaluatorId);
        builder.HasIndex(e => e.TriggeringInjectId);
        builder.HasIndex(e => e.ObservedAt);
        builder.HasIndex(e => new { e.CriticalTaskId, e.ObservedAt });

        // Relationship to CriticalTask (cascade delete when task is deleted)
        builder.HasOne(e => e.CriticalTask)
            .WithMany(ct => ct.EegEntries)
            .HasForeignKey(e => e.CriticalTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to ApplicationUser (evaluator)
        builder.HasOne(e => e.Evaluator)
            .WithMany()
            .HasForeignKey(e => e.EvaluatorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Relationship to Inject (triggering inject, optional)
        builder.HasOne(e => e.TriggeringInject)
            .WithMany(i => i.TriggeredEegEntries)
            .HasForeignKey(e => e.TriggeringInjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Relationship to Organization (for data isolation)
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
