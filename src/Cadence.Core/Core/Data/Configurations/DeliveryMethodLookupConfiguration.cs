using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cadence.Core.Data.Configurations;

public class DeliveryMethodLookupConfiguration : IEntityTypeConfiguration<DeliveryMethodLookup>
{
    public void Configure(EntityTypeBuilder<DeliveryMethodLookup> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        // Unique index on Name since these are system-level reference data
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.SortOrder);

        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed system default delivery methods with deterministic GUIDs
        builder.HasData(
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Verbal",
                Description = "Spoken directly to player",
                IsActive = true, SortOrder = 1, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Phone",
                Description = "Simulated phone call",
                IsActive = true, SortOrder = 2, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "Email",
                Description = "Simulated email",
                IsActive = true, SortOrder = 3, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Name = "Radio",
                Description = "Radio communication",
                IsActive = true, SortOrder = 4, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                Name = "Written",
                Description = "Paper document",
                IsActive = true, SortOrder = 5, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                Name = "Simulation",
                Description = "CAX/simulation input",
                IsActive = true, SortOrder = 6, IsOther = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            },
            new DeliveryMethodLookup
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                Name = "Other",
                Description = "Custom delivery method (specify in notes)",
                IsActive = true, SortOrder = 99, IsOther = true,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = seedDate, UpdatedAt = seedDate
            }
        );
    }
}
