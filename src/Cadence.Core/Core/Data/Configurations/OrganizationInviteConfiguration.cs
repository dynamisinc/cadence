using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class OrganizationInviteConfiguration : IEntityTypeConfiguration<OrganizationInvite>
{
    public void Configure(EntityTypeBuilder<OrganizationInvite> builder)
    {
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.UsedById).HasMaxLength(450);
        builder.Property(e => e.CreatedByUserId).HasMaxLength(450).IsRequired();

        // Unique index on Code
        builder.HasIndex(e => e.Code).IsUnique();

        // Indexes for common queries
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => new { e.OrganizationId, e.ExpiresAt });
        builder.HasIndex(e => e.Email);

        // Relationships
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Invites)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UsedBy)
            .WithMany()
            .HasForeignKey(e => e.UsedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
