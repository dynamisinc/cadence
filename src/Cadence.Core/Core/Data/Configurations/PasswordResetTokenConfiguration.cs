using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(e => e.IpAddress).HasMaxLength(50);

        // Indexes for efficient token lookup and cleanup
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.TokenHash);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.UserId, e.UsedAt });

        // Relationship to ApplicationUser
        builder.HasOne(e => e.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
