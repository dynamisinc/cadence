using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class OrganizationSuggestionConfiguration : IEntityTypeConfiguration<OrganizationSuggestion>
{
    public void Configure(EntityTypeBuilder<OrganizationSuggestion> builder)
    {
        builder.Property(e => e.FieldName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Value).HasMaxLength(500).IsRequired();

        // Unique constraint: one value per field per organization
        builder.HasIndex(e => new { e.OrganizationId, e.FieldName, e.Value }).IsUnique();

        // Index for common query: get active suggestions for a field
        builder.HasIndex(e => new { e.OrganizationId, e.FieldName, e.IsActive, e.SortOrder });

        // Relationship
        builder.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
