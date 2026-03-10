using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.ContactEmail).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.InjectApprovalPolicy).HasConversion<string>().HasMaxLength(20);

        // Unique index on Slug
        builder.HasIndex(e => e.Slug).IsUnique();

        // Index for status queries
        builder.HasIndex(e => e.Status);

        // Seed default organization
        builder.HasData(new Organization
        {
            Id = SystemConstants.DefaultOrganizationId,
            Name = "Default Organization",
            Slug = "default",
            Description = "Default organization for the Cadence system",
            Status = OrgStatus.Active,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
