using Cadence.Core.Features.Organizations.Mappers;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Mappers;

public class OrganizationMapperTests
{
    private static Organization CreateTestOrganization() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Metro Fire Department",
        Slug = "metro-fire",
        Description = "Metropolitan fire rescue operations",
        ContactEmail = "admin@metrofire.gov",
        Status = OrgStatus.Active,
        InjectApprovalPolicy = ApprovalPolicy.Optional,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ToDto Tests
    // =========================================================================

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var org = CreateTestOrganization();

        var dto = org.ToDto();

        dto.Id.Should().Be(org.Id);
        dto.Name.Should().Be("Metro Fire Department");
        dto.Slug.Should().Be("metro-fire");
        dto.Description.Should().Be("Metropolitan fire rescue operations");
        dto.ContactEmail.Should().Be("admin@metrofire.gov");
        dto.Status.Should().Be("Active");
        dto.InjectApprovalPolicy.Should().Be("Optional");
        dto.CreatedAt.Should().Be(org.CreatedAt);
        dto.UpdatedAt.Should().Be(org.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullOptionalFields_MapsAsNull()
    {
        var org = CreateTestOrganization();
        org.Description = null;
        org.ContactEmail = null;

        var dto = org.ToDto();

        dto.Description.Should().BeNull();
        dto.ContactEmail.Should().BeNull();
    }

    [Fact]
    public void ToDto_DifferentStatuses_MapsCorrectly()
    {
        var org = CreateTestOrganization();
        org.Status = OrgStatus.Archived;

        var dto = org.ToDto();

        dto.Status.Should().Be("Archived");
    }

    [Fact]
    public void ToDto_DifferentApprovalPolicies_MapsCorrectly()
    {
        var org = CreateTestOrganization();
        org.InjectApprovalPolicy = ApprovalPolicy.Required;

        var dto = org.ToDto();

        dto.InjectApprovalPolicy.Should().Be("Required");
    }

    // =========================================================================
    // ToListItemDto Tests
    // =========================================================================

    [Fact]
    public void ToListItemDto_MapsAllProperties()
    {
        var org = CreateTestOrganization();

        var dto = org.ToListItemDto(userCount: 25, exerciseCount: 10);

        dto.Id.Should().Be(org.Id);
        dto.Name.Should().Be("Metro Fire Department");
        dto.Slug.Should().Be("metro-fire");
        dto.Status.Should().Be("Active");
        dto.UserCount.Should().Be(25);
        dto.ExerciseCount.Should().Be(10);
        dto.CreatedAt.Should().Be(org.CreatedAt);
    }

    [Fact]
    public void ToListItemDto_ZeroCounts_MapsCorrectly()
    {
        var org = CreateTestOrganization();

        var dto = org.ToListItemDto(userCount: 0, exerciseCount: 0);

        dto.UserCount.Should().Be(0);
        dto.ExerciseCount.Should().Be(0);
    }
}
