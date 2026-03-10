using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ExerciseParticipantConfiguration : IEntityTypeConfiguration<ExerciseParticipant>
{
    public void Configure(EntityTypeBuilder<ExerciseParticipant> builder)
    {
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.AssignedAt).IsRequired();

        // Unique constraint: one role per user per exercise
        builder.HasIndex(e => new { e.ExerciseId, e.UserId }).IsUnique();
        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.Participants)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // User navigation references ApplicationUser (ASP.NET Core Identity)
        // Optional to handle deactivated users gracefully
        builder.HasOne(e => e.User)
            .WithMany(u => u.ExerciseParticipations)
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Assigned by user (for audit trail)
        builder.HasOne(e => e.AssignedBy)
            .WithMany()
            .HasForeignKey(e => e.AssignedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
