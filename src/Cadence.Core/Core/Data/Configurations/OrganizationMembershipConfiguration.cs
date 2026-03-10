using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.Property(e => e.UserId).HasMaxLength(450).IsRequired();
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.InvitedById).HasMaxLength(450);

        // Unique constraint: one membership per user per organization
        builder.HasIndex(e => new { e.UserId, e.OrganizationId }).IsUnique();

        // Indexes for common queries
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => new { e.OrganizationId, e.Status });

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Memberships)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.InvitedBy)
            .WithMany()
            .HasForeignKey(e => e.InvitedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
