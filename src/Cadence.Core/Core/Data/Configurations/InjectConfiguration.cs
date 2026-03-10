using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class InjectConfiguration : IEntityTypeConfiguration<Inject>
{
    public void Configure(EntityTypeBuilder<Inject> builder)
    {
        // Core properties
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Target).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Source).HasMaxLength(200);

        // Delivery method (legacy enum - will be migrated)
        builder.Property(e => e.DeliveryMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.DeliveryMethodOther).HasMaxLength(100);

        // Enums
        builder.Property(e => e.InjectType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TriggerType).HasConversion<string>().HasMaxLength(20);

        // Text fields
        builder.Property(e => e.FireCondition).HasMaxLength(500);
        builder.Property(e => e.ExpectedAction).HasMaxLength(2000);
        builder.Property(e => e.ControllerNotes).HasMaxLength(2000);
        builder.Property(e => e.SkipReason).HasMaxLength(500);

        // Import & Excel properties
        builder.Property(e => e.SourceReference).HasMaxLength(50);
        builder.Property(e => e.ResponsibleController).HasMaxLength(200);
        builder.Property(e => e.LocationName).HasMaxLength(200);
        builder.Property(e => e.LocationType).HasMaxLength(100);
        builder.Property(e => e.Track).HasMaxLength(100);

        // Approval workflow properties
        builder.Property(e => e.ApproverNotes).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);
        builder.Property(e => e.RevertReason).HasMaxLength(1000);

        // Indexes
        builder.HasIndex(e => new { e.MselId, e.InjectNumber }).IsUnique();
        builder.HasIndex(e => new { e.MselId, e.Sequence });
        builder.HasIndex(e => new { e.MselId, e.Status });
        builder.HasIndex(e => e.PhaseId);
        builder.HasIndex(e => e.ParentInjectId);
        builder.HasIndex(e => e.Track);
        builder.HasIndex(e => e.DeliveryMethodId);
        builder.HasIndex(e => e.FiredByUserId);
        builder.HasIndex(e => e.SkippedByUserId);
        builder.HasIndex(e => e.SubmittedByUserId);
        builder.HasIndex(e => e.ApprovedByUserId);
        builder.HasIndex(e => e.RejectedByUserId);
        builder.HasIndex(e => e.RevertedByUserId);

        builder.HasOne(e => e.Msel)
            .WithMany(m => m.Injects)
            .HasForeignKey(e => e.MselId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Phase)
            .WithMany(p => p.Injects)
            .HasForeignKey(e => e.PhaseId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ParentInject)
            .WithMany(i => i.ChildInjects)
            .HasForeignKey(e => e.ParentInjectId)
            .OnDelete(DeleteBehavior.NoAction);

        // User references (ApplicationUser) are optional to handle deactivated users gracefully.
        builder.HasOne(e => e.FiredByUser)
            .WithMany()
            .HasForeignKey(e => e.FiredByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.SkippedByUser)
            .WithMany()
            .HasForeignKey(e => e.SkippedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.DeliveryMethodLookup)
            .WithMany(dm => dm.Injects)
            .HasForeignKey(e => e.DeliveryMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Approval workflow user references (ApplicationUser) are optional to handle deactivated users gracefully.
        builder.HasOne(e => e.SubmittedByUser)
            .WithMany()
            .HasForeignKey(e => e.SubmittedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.ApprovedByUser)
            .WithMany()
            .HasForeignKey(e => e.ApprovedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.RejectedByUser)
            .WithMany()
            .HasForeignKey(e => e.RejectedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.RevertedByUser)
            .WithMany()
            .HasForeignKey(e => e.RevertedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
