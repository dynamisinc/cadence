using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class CapabilityConfiguration : IEntityTypeConfiguration<Capability>
{
    public void Configure(EntityTypeBuilder<Capability> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.SourceLibrary).HasMaxLength(50);

        // Unique index on (OrganizationId, Name)
        builder.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.IsActive);

        // Covering index for common query: WHERE OrganizationId = @id AND IsActive = true ORDER BY Category, SortOrder, Name
        builder.HasIndex(e => new { e.OrganizationId, e.IsActive, e.Category, e.SortOrder, e.Name });

        // Relationship to Organization
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
