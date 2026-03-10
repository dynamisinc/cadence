using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> builder)
    {
        builder.Property(e => e.ObjectiveNumber).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasIndex(e => new { e.ExerciseId, e.ObjectiveNumber });

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.Objectives)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
