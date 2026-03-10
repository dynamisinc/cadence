using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class InjectStatusHistoryConfiguration : IEntityTypeConfiguration<InjectStatusHistory>
{
    public void Configure(EntityTypeBuilder<InjectStatusHistory> builder)
    {
        // Enums stored as strings for readability
        builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.ChangedByUserId).HasMaxLength(450).IsRequired();

        // Indexes for efficient queries
        builder.HasIndex(e => e.InjectId);
        builder.HasIndex(e => new { e.InjectId, e.ChangedAt });
        builder.HasIndex(e => e.ChangedByUserId);

        // Relationship to Inject
        builder.HasOne(e => e.Inject)
            .WithMany(i => i.StatusHistory)
            .HasForeignKey(e => e.InjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // User who made the change - references ApplicationUser (ASP.NET Core Identity)
        // Uses string FK to match IdentityUser.Id type
        builder.HasOne(e => e.ChangedByUser)
            .WithMany()
            .HasForeignKey(e => e.ChangedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
