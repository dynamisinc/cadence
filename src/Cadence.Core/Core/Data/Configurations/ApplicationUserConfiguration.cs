using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SystemRole).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        // Index for common queries
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.SystemRole);
        builder.HasIndex(e => e.OrganizationId);
        builder.HasIndex(e => e.CurrentOrganizationId);

        // Relationship to Organization (nullable for pending users)
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship to CurrentOrganization
        builder.HasOne(e => e.CurrentOrganization)
            .WithMany()
            .HasForeignKey(e => e.CurrentOrganizationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Self-referential relationship for tracking who created the user
        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
