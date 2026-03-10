using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class CriticalTaskConfiguration : IEntityTypeConfiguration<CriticalTask>
{
    public void Configure(EntityTypeBuilder<CriticalTask> builder)
    {
        builder.Property(e => e.TaskDescription).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Standard).HasMaxLength(1000);

        // Indexes for efficient queries
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.CapabilityTargetId);
        builder.HasIndex(e => new { e.CapabilityTargetId, e.SortOrder });

        // Relationship to Organization (required for multi-tenancy data isolation)
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to CapabilityTarget (cascade delete when target is deleted)
        builder.HasOne(e => e.CapabilityTarget)
            .WithMany(ct => ct.CriticalTasks)
            .HasForeignKey(e => e.CapabilityTargetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
