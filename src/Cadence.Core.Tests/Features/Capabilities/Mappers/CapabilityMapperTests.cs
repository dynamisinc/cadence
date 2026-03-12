using Cadence.Core.Features.Capabilities.Mappers;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Capabilities.Mappers;

/// <summary>
/// Unit tests for CapabilityMapper extension methods.
/// Verifies that all entity properties are correctly projected to DTOs
/// and that request objects are correctly applied to entities.
/// </summary>
public class CapabilityMapperTests
{
    // =========================================================================
    // ToDto
    // =========================================================================

    [Fact]
    public void ToDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Mass Care Services",
            Description = "Provides life-sustaining services to affected populations.",
            Category = "Response",
            SortOrder = 5,
            IsActive = true,
            SourceLibrary = "FEMA",
            CreatedAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 6, 20, 14, 30, 0, DateTimeKind.Utc)
        };

        // Act
        CapabilityDto dto = entity.ToDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.OrganizationId.Should().Be(entity.OrganizationId);
        dto.Name.Should().Be(entity.Name);
        dto.Description.Should().Be(entity.Description);
        dto.Category.Should().Be(entity.Category);
        dto.SortOrder.Should().Be(entity.SortOrder);
        dto.IsActive.Should().Be(entity.IsActive);
        dto.SourceLibrary.Should().Be(entity.SourceLibrary);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullablePropertiesAreNull_MapsNullsThrough()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Custom Capability",
            Description = null,
            Category = null,
            SortOrder = 0,
            IsActive = false,
            SourceLibrary = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        CapabilityDto dto = entity.ToDto();

        // Assert
        dto.Description.Should().BeNull();
        dto.Category.Should().BeNull();
        dto.SourceLibrary.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ToDto_SortOrderZero_MapsSortOrderCorrectly()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "First",
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        CapabilityDto dto = entity.ToDto();

        // Assert
        dto.SortOrder.Should().Be(0);
    }

    // =========================================================================
    // ToSummaryDto
    // =========================================================================

    [Fact]
    public void ToSummaryDto_AllPropertiesPopulated_MapsOnlyFourFields()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Cybersecurity",
            Description = "Protect against cyber threats.",
            Category = "Protection",
            SortOrder = 3,
            IsActive = true,
            SourceLibrary = "NIST",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        CapabilitySummaryDto dto = entity.ToSummaryDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.Name.Should().Be(entity.Name);
        dto.Category.Should().Be(entity.Category);
        dto.IsActive.Should().Be(entity.IsActive);
    }

    [Fact]
    public void ToSummaryDto_NullCategory_MapsNullCategory()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Custom",
            Category = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        CapabilitySummaryDto dto = entity.ToSummaryDto();

        // Assert
        dto.Category.Should().BeNull();
    }

    [Fact]
    public void ToSummaryDto_InactiveCapability_MapsIsActiveFalse()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Legacy Capability",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        CapabilitySummaryDto dto = entity.ToSummaryDto();

        // Assert
        dto.IsActive.Should().BeFalse();
    }

    // =========================================================================
    // ToEntity
    // =========================================================================

    [Fact]
    public void ToEntity_ValidRequest_MapsAllRequestFields()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = new CreateCapabilityRequest
        {
            Name = "  Mass Care Services  ",
            Description = "  Life-sustaining services.  ",
            Category = "  Response  ",
            SortOrder = 7,
            SourceLibrary = "  FEMA  "
        };

        // Act
        Capability entity = request.ToEntity(orgId);

        // Assert
        entity.OrganizationId.Should().Be(orgId);
        entity.Name.Should().Be("Mass Care Services");
        entity.Description.Should().Be("Life-sustaining services.");
        entity.Category.Should().Be("Response");
        entity.SortOrder.Should().Be(7);
        entity.IsActive.Should().BeTrue("new capabilities are active by default");
        entity.SourceLibrary.Should().Be("FEMA");
    }

    [Fact]
    public void ToEntity_RequestWithNullOptionals_MapsNullsThrough()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = new CreateCapabilityRequest
        {
            Name = "Custom",
            Description = null,
            Category = null,
            SortOrder = 0,
            SourceLibrary = null
        };

        // Act
        Capability entity = request.ToEntity(orgId);

        // Assert
        entity.Description.Should().BeNull();
        entity.Category.Should().BeNull();
        entity.SourceLibrary.Should().BeNull();
    }

    [Fact]
    public void ToEntity_ValidRequest_GeneratesNewId()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var request = new CreateCapabilityRequest { Name = "Test" };

        // Act
        Capability entity1 = request.ToEntity(orgId);
        Capability entity2 = request.ToEntity(orgId);

        // Assert
        entity1.Id.Should().NotBeEmpty();
        entity2.Id.Should().NotBeEmpty();
        entity1.Id.Should().NotBe(entity2.Id, "each call should generate a new unique Id");
    }

    [Fact]
    public void ToEntity_ValidRequest_SetsTimestamps()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = new CreateCapabilityRequest { Name = "Test" };

        // Act
        Capability entity = request.ToEntity(Guid.NewGuid());
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        entity.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
        entity.UpdatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void ToEntity_NameWithLeadingTrailingWhitespace_TrimsName()
    {
        // Arrange
        var request = new CreateCapabilityRequest { Name = "   Trimmed Name   " };

        // Act
        Capability entity = request.ToEntity(Guid.NewGuid());

        // Assert
        entity.Name.Should().Be("Trimmed Name");
    }

    // =========================================================================
    // UpdateFromRequest
    // =========================================================================

    [Fact]
    public void UpdateFromRequest_ValidRequest_UpdatesAllWritableFields()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Old Name",
            Description = "Old description",
            Category = "Old Category",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var request = new UpdateCapabilityRequest
        {
            Name = "  New Name  ",
            Description = "  New description.  ",
            Category = "  New Category  ",
            SortOrder = 10,
            IsActive = false
        };

        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        entity.UpdateFromRequest(request);

        // Assert
        entity.Name.Should().Be("New Name");
        entity.Description.Should().Be("New description.");
        entity.Category.Should().Be("New Category");
        entity.SortOrder.Should().Be(10);
        entity.IsActive.Should().BeFalse();
        entity.UpdatedAt.Should().BeAfter(beforeUpdate);
    }

    [Fact]
    public void UpdateFromRequest_NullOptionals_SetsNulls()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Old",
            Description = "Old description",
            Category = "Old category",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateCapabilityRequest
        {
            Name = "New",
            Description = null,
            Category = null,
            SortOrder = 0,
            IsActive = true
        };

        // Act
        entity.UpdateFromRequest(request);

        // Assert
        entity.Description.Should().BeNull();
        entity.Category.Should().BeNull();
    }

    [Fact]
    public void UpdateFromRequest_NameWithWhitespace_TrimsName()
    {
        // Arrange
        var entity = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Old",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateCapabilityRequest { Name = "   Trimmed   ", IsActive = true };

        // Act
        entity.UpdateFromRequest(request);

        // Assert
        entity.Name.Should().Be("Trimmed");
    }

    [Fact]
    public void UpdateFromRequest_DoesNotChangeId_OrOrganizationId()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var originalOrgId = Guid.NewGuid();

        var entity = new Capability
        {
            Id = originalId,
            OrganizationId = originalOrgId,
            Name = "Old",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new UpdateCapabilityRequest { Name = "New", IsActive = true };

        // Act
        entity.UpdateFromRequest(request);

        // Assert
        entity.Id.Should().Be(originalId, "UpdateFromRequest must not modify the entity Id");
        entity.OrganizationId.Should().Be(originalOrgId, "UpdateFromRequest must not modify OrganizationId");
    }
}
