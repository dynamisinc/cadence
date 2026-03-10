using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Abbreviation).HasMaxLength(20);
        builder.Property(e => e.Description).HasMaxLength(500);

        // Unique constraint: one agency name per organization
        builder.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();

        // Indexes for common queries
        builder.HasIndex(e => new { e.OrganizationId, e.IsActive, e.SortOrder });

        // Relationship
        builder.HasOne(e => e.Organization)
            .WithMany(o => o.Agencies)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
