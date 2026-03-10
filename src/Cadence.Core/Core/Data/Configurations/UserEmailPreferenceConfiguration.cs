using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class UserEmailPreferenceConfiguration : IEntityTypeConfiguration<UserEmailPreference>
{
    public void Configure(EntityTypeBuilder<UserEmailPreference> builder)
    {
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);

        // Unique constraint: one preference per user per category
        builder.HasIndex(e => new { e.UserId, e.Category }).IsUnique();

        // Index for user lookups
        builder.HasIndex(e => e.UserId);

        // Relationship to ApplicationUser
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
