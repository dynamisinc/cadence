using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ExpectedOutcomeConfiguration : IEntityTypeConfiguration<ExpectedOutcome>
{
    public void Configure(EntityTypeBuilder<ExpectedOutcome> builder)
    {
        builder.Property(e => e.Description).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.EvaluatorNotes).HasMaxLength(2000);

        builder.HasIndex(e => new { e.InjectId, e.SortOrder });

        builder.HasOne(e => e.Inject)
            .WithMany(i => i.ExpectedOutcomes)
            .HasForeignKey(e => e.InjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
