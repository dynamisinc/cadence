using Cadence.Core.Features.Organizations.Mappers;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Organizations.Mappers;

/// <summary>
/// Unit tests for OrganizationMapper extension methods.
/// Verifies correct projection of Organization entities to OrganizationDto and OrganizationListItemDto.
/// </summary>
public class OrganizationMapperTests
{
    // =========================================================================
    // ToDto
    // =========================================================================

    [Fact]
    public void ToDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Acme Emergency Services",
            Slug = "acme-emergency",
            Description = "Regional emergency management agency.",
            ContactEmail = "admin@acme.gov",
            Status = OrgStatus.Active,
            InjectApprovalPolicy = ApprovalPolicy.Required,
            CreatedAt = new DateTime(2024, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 10, 16, 45, 0, DateTimeKind.Utc)
        };

        // Act
        OrganizationDto dto = organization.ToDto();

        // Assert
        dto.Id.Should().Be(organization.Id);
        dto.Name.Should().Be(organization.Name);
        dto.Slug.Should().Be(organization.Slug);
        dto.Description.Should().Be(organization.Description);
        dto.ContactEmail.Should().Be(organization.ContactEmail);
        dto.Status.Should().Be(OrgStatus.Active.ToString());
        dto.InjectApprovalPolicy.Should().Be(ApprovalPolicy.Required.ToString());
        dto.CreatedAt.Should().Be(organization.CreatedAt);
        dto.UpdatedAt.Should().Be(organization.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullablePropertiesAreNull_MapsNullsThrough()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Minimal Org",
            Slug = "minimal-org",
            Description = null,
            ContactEmail = null,
            Status = OrgStatus.Inactive,
            InjectApprovalPolicy = ApprovalPolicy.Disabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationDto dto = organization.ToDto();

        // Assert
        dto.Description.Should().BeNull();
        dto.ContactEmail.Should().BeNull();
    }

    [Fact]
    public void ToDto_StatusArchivedOrg_MapsStatusAsString()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Archived Org",
            Slug = "archived-org",
            Status = OrgStatus.Archived,
            InjectApprovalPolicy = ApprovalPolicy.Optional,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationDto dto = organization.ToDto();

        // Assert
        dto.Status.Should().Be("Archived");
    }

    [Fact]
    public void ToDto_ApprovalPolicyOptional_MapsApprovalPolicyAsString()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Optional Org",
            Slug = "optional-org",
            Status = OrgStatus.Active,
            InjectApprovalPolicy = ApprovalPolicy.Optional,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationDto dto = organization.ToDto();

        // Assert
        dto.InjectApprovalPolicy.Should().Be("Optional");
    }

    // =========================================================================
    // ToListItemDto
    // =========================================================================

    [Fact]
    public void ToListItemDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Regional FEMA Agency",
            Slug = "regional-fema",
            Status = OrgStatus.Active,
            CreatedAt = new DateTime(2023, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = DateTime.UtcNow
        };
        const int userCount = 42;
        const int exerciseCount = 7;

        // Act
        OrganizationListItemDto dto = organization.ToListItemDto(userCount, exerciseCount);

        // Assert
        dto.Id.Should().Be(organization.Id);
        dto.Name.Should().Be(organization.Name);
        dto.Slug.Should().Be(organization.Slug);
        dto.Status.Should().Be(OrgStatus.Active.ToString());
        dto.UserCount.Should().Be(userCount);
        dto.ExerciseCount.Should().Be(exerciseCount);
        dto.CreatedAt.Should().Be(organization.CreatedAt);
    }

    [Fact]
    public void ToListItemDto_ZeroCounts_MapsZeroes()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Empty Org",
            Slug = "empty-org",
            Status = OrgStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationListItemDto dto = organization.ToListItemDto(0, 0);

        // Assert
        dto.UserCount.Should().Be(0);
        dto.ExerciseCount.Should().Be(0);
    }

    [Fact]
    public void ToListItemDto_StatusInactive_MapsStatusAsString()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Inactive Org",
            Slug = "inactive-org",
            Status = OrgStatus.Inactive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationListItemDto dto = organization.ToListItemDto(1, 2);

        // Assert
        dto.Status.Should().Be("Inactive");
    }

    [Fact]
    public void ToListItemDto_DoesNotIncludeDescriptionOrContactEmail()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            Slug = "test-org",
            Description = "Should not appear in list item DTO",
            ContactEmail = "noreply@example.com",
            Status = OrgStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationListItemDto dto = organization.ToListItemDto(5, 3);

        // Assert — OrganizationListItemDto record does not have Description or ContactEmail members
        // Confirming this via the type: the DTO only has Id, Name, Slug, Status, UserCount, ExerciseCount, CreatedAt
        dto.Should().NotBeNull();
        var dtoType = dto.GetType();
        dtoType.GetProperty("Description").Should().BeNull(
            "OrganizationListItemDto must not expose Description");
        dtoType.GetProperty("ContactEmail").Should().BeNull(
            "OrganizationListItemDto must not expose ContactEmail");
    }

    // =========================================================================
    // Round-trip consistency
    // =========================================================================

    [Fact]
    public void ToDto_AndToListItemDto_ProduceConsistentIdNameSlug()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Consistent Org",
            Slug = "consistent-org",
            Status = OrgStatus.Active,
            InjectApprovalPolicy = ApprovalPolicy.Optional,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        OrganizationDto fullDto = organization.ToDto();
        OrganizationListItemDto listDto = organization.ToListItemDto(10, 5);

        // Assert
        fullDto.Id.Should().Be(listDto.Id);
        fullDto.Name.Should().Be(listDto.Name);
        fullDto.Slug.Should().Be(listDto.Slug);
        fullDto.Status.Should().Be(listDto.Status);
        fullDto.CreatedAt.Should().Be(listDto.CreatedAt);
    }
}
