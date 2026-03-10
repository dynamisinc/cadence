using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(e => e.CreatedByIp).HasMaxLength(50);
        builder.Property(e => e.DeviceInfo).HasMaxLength(200);

        // Indexes for efficient token lookup and cleanup
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.TokenHash);
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => new { e.UserId, e.IsRevoked });

        // Relationship to ApplicationUser
        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
