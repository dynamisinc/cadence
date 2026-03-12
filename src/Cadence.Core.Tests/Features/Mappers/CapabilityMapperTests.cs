using Cadence.Core.Features.Capabilities.Mappers;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Mappers;

public class CapabilityMapperTests
{
    private static Capability CreateTestCapability() => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Name = "Mass Care Services",
        Description = "Shelter and feeding operations",
        Category = "Response",
        SortOrder = 5,
        IsActive = true,
        SourceLibrary = "FEMA Core Capabilities",
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ToDto Tests
    // =========================================================================

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var entity = CreateTestCapability();

        var dto = entity.ToDto();

        dto.Id.Should().Be(entity.Id);
        dto.OrganizationId.Should().Be(entity.OrganizationId);
        dto.Name.Should().Be("Mass Care Services");
        dto.Description.Should().Be("Shelter and feeding operations");
        dto.Category.Should().Be("Response");
        dto.SortOrder.Should().Be(5);
        dto.IsActive.Should().BeTrue();
        dto.SourceLibrary.Should().Be("FEMA Core Capabilities");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullOptionalFields_MapsAsNull()
    {
        var entity = CreateTestCapability();
        entity.Description = null;
        entity.Category = null;
        entity.SourceLibrary = null;

        var dto = entity.ToDto();

        dto.Description.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.SourceLibrary.Should().BeNull();
    }

    // =========================================================================
    // ToSummaryDto Tests
    // =========================================================================

    [Fact]
    public void ToSummaryDto_MapsCorrectSubset()
    {
        var entity = CreateTestCapability();

        var dto = entity.ToSummaryDto();

        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be("Mass Care Services");
        dto.Category.Should().Be("Response");
        dto.IsActive.Should().BeTrue();
    }

    // =========================================================================
    // ToEntity Tests
    // =========================================================================

    [Fact]
    public void ToEntity_MapsRequestFields_TrimsWhitespace()
    {
        var orgId = Guid.NewGuid();
        var request = new CreateCapabilityRequest
        {
            Name = "  Cybersecurity  ",
            Description = "  Network defense  ",
            Category = "  Protection  ",
            SortOrder = 3,
            SourceLibrary = "  Custom Library  "
        };

        var entity = request.ToEntity(orgId);

        entity.Id.Should().NotBeEmpty();
        entity.OrganizationId.Should().Be(orgId);
        entity.Name.Should().Be("Cybersecurity");
        entity.Description.Should().Be("Network defense");
        entity.Category.Should().Be("Protection");
        entity.SortOrder.Should().Be(3);
        entity.IsActive.Should().BeTrue();
        entity.SourceLibrary.Should().Be("Custom Library");
    }

    // =========================================================================
    // UpdateFromRequest Tests
    // =========================================================================

    [Fact]
    public void UpdateFromRequest_UpdatesAllFields()
    {
        var entity = CreateTestCapability();
        var request = new UpdateCapabilityRequest
        {
            Name = "  Updated Name  ",
            Description = "  Updated Desc  ",
            Category = "  Recovery  ",
            SortOrder = 10,
            IsActive = false
        };

        entity.UpdateFromRequest(request);

        entity.Name.Should().Be("Updated Name");
        entity.Description.Should().Be("Updated Desc");
        entity.Category.Should().Be("Recovery");
        entity.SortOrder.Should().Be(10);
        entity.IsActive.Should().BeFalse();
    }
}
