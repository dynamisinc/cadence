using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class MselConfiguration : IEntityTypeConfiguration<Msel>
{
    public void Configure(EntityTypeBuilder<Msel> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);

        builder.HasIndex(e => new { e.ExerciseId, e.Version });

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.Msels)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
