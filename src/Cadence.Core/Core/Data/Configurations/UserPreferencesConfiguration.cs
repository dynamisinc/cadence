using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        // Use UserId as primary key (1:1 relationship with ApplicationUser)
        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).HasMaxLength(450);

        // Enums stored as strings for readability
        builder.Property(e => e.Theme).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.DisplayDensity).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.TimeFormat).HasConversion<string>().HasMaxLength(20);

        // One-to-one relationship with ApplicationUser
        builder.HasOne(e => e.User)
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
