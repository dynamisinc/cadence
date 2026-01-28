using Cadence.Core.Data;
using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Capabilities;

public class CapabilityServiceTests
{
    private readonly Mock<ILogger<CapabilityService>> _loggerMock;

    public CapabilityServiceTests()
    {
        _loggerMock = new Mock<ILogger<CapabilityService>>();
    }

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private Capability CreateCapability(
        AppDbContext context,
        Organization org,
        string name = "Test Capability",
        string? category = "Response",
        bool isActive = true,
        string? sourceLibrary = null)
    {
        var capability = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = name,
            Description = "Test description",
            Category = category,
            SortOrder = 0,
            IsActive = isActive,
            SourceLibrary = sourceLibrary
        };
        context.Capabilities.Add(capability);
        context.SaveChanges();

        return capability;
    }

    private CapabilityService CreateService(AppDbContext context)
    {
        return new CapabilityService(context, _loggerMock.Object);
    }

    #region GetCapabilitiesAsync Tests

    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsOnlyActiveByDefault()
    {
        var (context, org) = CreateTestContext();
        var activeCapability = CreateCapability(context, org, "Active Capability", isActive: true);
        var inactiveCapability = CreateCapability(context, org, "Inactive Capability", isActive: false);
        var service = CreateService(context);

        var result = await service.GetCapabilitiesAsync(org.Id);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Capability");
    }

    [Fact]
    public async Task GetCapabilitiesAsync_WithIncludeInactive_ReturnsAll()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Active Capability", isActive: true);
        CreateCapability(context, org, "Inactive Capability", isActive: false);
        var service = CreateService(context);

        var result = await service.GetCapabilitiesAsync(org.Id, includeInactive: true);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsEmptyForOtherOrganization()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Test Capability");
        var service = CreateService(context);

        var result = await service.GetCapabilitiesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCapabilitiesAsync_OrdersByCategoryThenSortOrderThenName()
    {
        var (context, org) = CreateTestContext();

        // Create capabilities in non-alphabetical order
        var cap1 = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = "Zebra Capability",
            Category = "Prevention",
            SortOrder = 1,
            IsActive = true
        };
        var cap2 = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = "Alpha Capability",
            Category = "Prevention",
            SortOrder = 0,
            IsActive = true
        };
        var cap3 = new Capability
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Name = "Response Capability",
            Category = "Response",
            SortOrder = 0,
            IsActive = true
        };

        context.Capabilities.AddRange(cap1, cap2, cap3);
        context.SaveChanges();

        var service = CreateService(context);

        var result = (await service.GetCapabilitiesAsync(org.Id)).ToList();

        // Prevention comes before Response alphabetically
        result[0].Name.Should().Be("Alpha Capability"); // Prevention, SortOrder 0
        result[1].Name.Should().Be("Zebra Capability"); // Prevention, SortOrder 1
        result[2].Name.Should().Be("Response Capability"); // Response, SortOrder 0
    }

    #endregion

    #region GetCapabilityAsync Tests

    [Fact]
    public async Task GetCapabilityAsync_ExistingCapability_ReturnsDto()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability", "Response");
        var service = CreateService(context);

        var result = await service.GetCapabilityAsync(org.Id, capability.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Capability");
        result.Category.Should().Be("Response");
    }

    [Fact]
    public async Task GetCapabilityAsync_NonExistentId_ReturnsNull()
    {
        var (context, org) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetCapabilityAsync(org.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCapabilityAsync_WrongOrganization_ReturnsNull()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability");
        var service = CreateService(context);

        var result = await service.GetCapabilityAsync(Guid.NewGuid(), capability.Id);

        result.Should().BeNull();
    }

    #endregion

    #region CreateCapabilityAsync Tests

    [Fact]
    public async Task CreateCapabilityAsync_ValidRequest_ReturnsCreatedDto()
    {
        var (context, org) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateCapabilityRequest
        {
            Name = "New Capability",
            Description = "A test capability",
            Category = "Response",
            SortOrder = 5,
            SourceLibrary = "FEMA"
        };

        var result = await service.CreateCapabilityAsync(org.Id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Capability");
        result.Description.Should().Be("A test capability");
        result.Category.Should().Be("Response");
        result.SortOrder.Should().Be(5);
        result.SourceLibrary.Should().Be("FEMA");
        result.IsActive.Should().BeTrue();
        result.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task CreateCapabilityAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);
        var request = new CreateCapabilityRequest
        {
            Name = "Existing Capability",
            Description = "Duplicate"
        };

        var act = async () => await service.CreateCapabilityAsync(org.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateCapabilityAsync_DuplicateNameCaseInsensitive_ThrowsInvalidOperationException()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);
        var request = new CreateCapabilityRequest
        {
            Name = "EXISTING CAPABILITY",
            Description = "Duplicate with different case"
        };

        var act = async () => await service.CreateCapabilityAsync(org.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateCapabilityAsync_SameNameDifferentOrganization_Succeeds()
    {
        var (context, org1) = CreateTestContext();
        CreateCapability(context, org1, "Shared Name");

        // Create second organization
        var org2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Second Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org2);
        context.SaveChanges();

        var service = CreateService(context);
        var request = new CreateCapabilityRequest
        {
            Name = "Shared Name",
            Description = "Same name, different org"
        };

        var result = await service.CreateCapabilityAsync(org2.Id, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Shared Name");
        result.OrganizationId.Should().Be(org2.Id);
    }

    [Fact]
    public async Task CreateCapabilityAsync_NonExistentOrganization_ThrowsInvalidOperationException()
    {
        var (context, _) = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateCapabilityRequest
        {
            Name = "New Capability"
        };

        var act = async () => await service.CreateCapabilityAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region UpdateCapabilityAsync Tests

    [Fact]
    public async Task UpdateCapabilityAsync_ValidRequest_UpdatesCapability()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Original Name", "Prevention");
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "Updated Name",
            Description = "Updated description",
            Category = "Response",
            SortOrder = 10,
            IsActive = true
        };

        var result = await service.UpdateCapabilityAsync(org.Id, capability.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.Category.Should().Be("Response");
        result.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task UpdateCapabilityAsync_NonExistentCapability_ReturnsNull()
    {
        var (context, org) = CreateTestContext();
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "Updated Name"
        };

        var result = await service.UpdateCapabilityAsync(org.Id, Guid.NewGuid(), request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCapabilityAsync_WrongOrganization_ReturnsNull()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability");
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "Updated Name"
        };

        var result = await service.UpdateCapabilityAsync(Guid.NewGuid(), capability.Id, request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCapabilityAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Other Capability");
        var capability = CreateCapability(context, org, "Original Name");
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "Other Capability",
            IsActive = true
        };

        var act = async () => await service.UpdateCapabilityAsync(org.Id, capability.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateCapabilityAsync_SameName_Succeeds()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Original Name");
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "Original Name",
            Description = "Updated description",
            IsActive = true
        };

        var result = await service.UpdateCapabilityAsync(org.Id, capability.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Original Name");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateCapabilityAsync_SameNameDifferentCase_Succeeds()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Original Name");
        var service = CreateService(context);
        var request = new UpdateCapabilityRequest
        {
            Name = "ORIGINAL NAME",
            Description = "Updated description",
            IsActive = true
        };

        var result = await service.UpdateCapabilityAsync(org.Id, capability.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("ORIGINAL NAME");
    }

    #endregion

    #region DeactivateCapabilityAsync Tests

    [Fact]
    public async Task DeactivateCapabilityAsync_ExistingCapability_SetsIsActiveFalse()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability", isActive: true);
        var service = CreateService(context);

        var result = await service.DeactivateCapabilityAsync(org.Id, capability.Id);

        result.Should().BeTrue();

        // Verify the capability is deactivated in database
        var updated = await context.Capabilities.FindAsync(capability.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateCapabilityAsync_NonExistentCapability_ReturnsFalse()
    {
        var (context, org) = CreateTestContext();
        var service = CreateService(context);

        var result = await service.DeactivateCapabilityAsync(org.Id, Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateCapabilityAsync_WrongOrganization_ReturnsFalse()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability");
        var service = CreateService(context);

        var result = await service.DeactivateCapabilityAsync(Guid.NewGuid(), capability.Id);

        result.Should().BeFalse();

        // Verify the capability is still active
        var updated = await context.Capabilities.FindAsync(capability.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateCapabilityAsync_PreservesCapabilityData()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Test Capability", "Response", isActive: true, "FEMA");
        var service = CreateService(context);

        await service.DeactivateCapabilityAsync(org.Id, capability.Id);

        // Verify all data is preserved except IsActive
        var updated = await context.Capabilities.FindAsync(capability.Id);
        updated!.Name.Should().Be("Test Capability");
        updated.Category.Should().Be("Response");
        updated.SourceLibrary.Should().Be("FEMA");
        updated.IsActive.Should().BeFalse();
    }

    #endregion

    #region IsNameUniqueAsync Tests

    [Fact]
    public async Task IsNameUniqueAsync_UniqueName_ReturnsTrue()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org.Id, "New Capability");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNameUniqueAsync_DuplicateName_ReturnsFalse()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org.Id, "Existing Capability");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsNameUniqueAsync_CaseInsensitive_ReturnsFalse()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org.Id, "EXISTING CAPABILITY");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsNameUniqueAsync_WithExcludeId_ExcludesSelf()
    {
        var (context, org) = CreateTestContext();
        var capability = CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org.Id, "Existing Capability", excludeId: capability.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsNameUniqueAsync_TrimsWhitespace()
    {
        var (context, org) = CreateTestContext();
        CreateCapability(context, org, "Existing Capability");
        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org.Id, "  Existing Capability  ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsNameUniqueAsync_DifferentOrganization_ReturnsTrue()
    {
        var (context, org1) = CreateTestContext();
        CreateCapability(context, org1, "Existing Capability");

        // Create second organization
        var org2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Second Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org2);
        context.SaveChanges();

        var service = CreateService(context);

        var result = await service.IsNameUniqueAsync(org2.Id, "Existing Capability");

        result.Should().BeTrue();
    }

    #endregion
}
