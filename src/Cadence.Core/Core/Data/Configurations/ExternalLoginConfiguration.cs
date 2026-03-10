using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.Property(e => e.Provider).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProviderUserId).HasMaxLength(200).IsRequired();

        // Unique index to prevent duplicate external logins
        builder.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
        builder.HasIndex(e => e.UserId);

        // Relationship to ApplicationUser
        builder.HasOne(e => e.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
