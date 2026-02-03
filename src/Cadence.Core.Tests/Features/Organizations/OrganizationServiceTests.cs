using Cadence.Core.Data;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Organizations;

public class OrganizationServiceTests
{
    private readonly Mock<ILogger<OrganizationService>> _loggerMock;

    public OrganizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<OrganizationService>>();
    }

    private AppDbContext CreateTestContext()
    {
        return TestDbContextFactory.Create();
    }

    private OrganizationService CreateService(AppDbContext context)
    {
        return new OrganizationService(context, _loggerMock.Object);
    }

    private Organization CreateOrganization(
        AppDbContext context,
        string name = "Test Org",
        string slug = "test-org",
        OrgStatus status = OrgStatus.Active)
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Status = status,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);
        context.SaveChanges();
        return org;
    }

    private ApplicationUser CreateUser(
        AppDbContext context,
        string email = "test@example.com",
        UserStatus status = UserStatus.Active,
        Guid? orgId = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            DisplayName = "Test User",
            Status = status,
            SystemRole = SystemRole.User,
            OrganizationId = orgId ?? Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        context.ApplicationUsers.Add(user);
        context.SaveChanges();
        return user;
    }

    #region GetOrganizationsAsync Tests

    [Fact]
    public async Task GetOrganizationsAsync_ReturnsAllActive_WhenNoFilters()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Org 1", "org-1", OrgStatus.Active);
        CreateOrganization(context, "Org 2", "org-2", OrgStatus.Active);
        CreateOrganization(context, "Org 3", "org-3", OrgStatus.Archived);
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync();

        // Should return all organizations including seed data
        var testOrgs = items.Where(i => i.Slug.StartsWith("org-")).ToList();
        testOrgs.Should().HaveCount(3);
        testOrgs.Should().Contain(o => o.Name == "Org 1");
        testOrgs.Should().Contain(o => o.Name == "Org 2");
        testOrgs.Should().Contain(o => o.Name == "Org 3");
    }

    [Fact]
    public async Task GetOrganizationsAsync_FiltersBySearch_MatchesNameOrSlug()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "CISA Region 4", "cisa-r4");
        CreateOrganization(context, "State EMA", "state-ema");
        CreateOrganization(context, "Test Org", "test-org");
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync(search: "cisa");

        items.Should().HaveCount(1);
        items.First().Name.Should().Be("CISA Region 4");
    }

    [Fact]
    public async Task GetOrganizationsAsync_FiltersBySearch_CaseInsensitive()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "CISA Region 4", "cisa-r4");
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync(search: "REGION");

        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrganizationsAsync_FiltersByStatus_ReturnsOnlyMatchingStatus()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Active Org", "active", OrgStatus.Active);
        CreateOrganization(context, "Archived Org", "archived", OrgStatus.Archived);
        CreateOrganization(context, "Inactive Org", "inactive", OrgStatus.Inactive);
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync(status: OrgStatus.Archived);

        items.Should().HaveCount(1);
        items.First().Name.Should().Be("Archived Org");
    }

    [Fact]
    public async Task GetOrganizationsAsync_SortsByName_Ascending()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Zebra Org", "test-zebra");
        CreateOrganization(context, "Alpha Org", "test-alpha");
        CreateOrganization(context, "Beta Org", "test-beta");
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync(sortBy: "name", sortDir: "asc");

        var testOrgs = items.Where(i => i.Slug.StartsWith("test-")).OrderBy(i => i.Name).ToList();
        testOrgs[0].Name.Should().Be("Alpha Org");
        testOrgs[1].Name.Should().Be("Beta Org");
        testOrgs[2].Name.Should().Be("Zebra Org");
    }

    [Fact]
    public async Task GetOrganizationsAsync_SortsByName_Descending()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Zebra Org", "test-zebra");
        CreateOrganization(context, "Alpha Org", "test-alpha");
        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync(sortBy: "name", sortDir: "desc");

        var testOrgs = items.Where(i => i.Slug.StartsWith("test-")).OrderByDescending(i => i.Name).ToList();
        testOrgs[0].Name.Should().Be("Zebra Org");
        testOrgs[1].Name.Should().Be("Alpha Org");
    }

    [Fact]
    public async Task GetOrganizationsAsync_IncludesUserCount()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test");
        var user1 = CreateUser(context, "user1@test.com");
        var user2 = CreateUser(context, "user2@test.com");

        // Add memberships
        context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = user1.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        });
        context.OrganizationMemberships.Add(new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = user2.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        });
        context.SaveChanges();

        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync();

        var testOrg = items.First(i => i.Id == org.Id);
        testOrg.UserCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrganizationsAsync_IncludesExerciseCount()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test");

        // Add exercises
        context.Exercises.Add(new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercise 1",
            ExerciseType = ExerciseType.TTX,
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        });
        context.Exercises.Add(new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Exercise 2",
            ExerciseType = ExerciseType.FE,
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        });
        context.SaveChanges();

        var service = CreateService(context);

        var (items, totalCount) = await service.GetOrganizationsAsync();

        var testOrg = items.First(i => i.Id == org.Id);
        testOrg.ExerciseCount.Should().Be(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingOrganization_ReturnsDto()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test-org");
        var service = CreateService(context);

        var result = await service.GetByIdAsync(org.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Org");
        result.Slug.Should().Be("test-org");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithExistingUser_CreatesOrgAndMembership()
    {
        var context = CreateTestContext();
        var existingUser = CreateUser(context, "admin@test.com");
        var service = CreateService(context);
        var request = new CreateOrganizationRequest(
            "New Org",
            "new-org",
            "Description",
            "contact@test.com",
            "admin@test.com"
        );
        var createdBy = Guid.NewGuid();

        var result = await service.CreateAsync(request, createdBy.ToString());

        result.Should().NotBeNull();
        result.Name.Should().Be("New Org");
        result.Slug.Should().Be("new-org");
        result.Status.Should().Be(OrgStatus.Active.ToString());

        // Verify membership was created
        var membership = context.OrganizationMemberships
            .FirstOrDefault(m => m.OrganizationId == result.Id && m.UserId == existingUser.Id);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(OrgRole.OrgAdmin);
    }

    [Fact]
    public async Task CreateAsync_WithExistingUser_ActivatesPendingUser()
    {
        var context = CreateTestContext();
        var pendingUser = CreateUser(context, "pending@test.com", UserStatus.Pending);
        var service = CreateService(context);
        var request = new CreateOrganizationRequest(
            "New Org",
            "new-org",
            null,
            null,
            "pending@test.com"
        );
        var createdBy = Guid.NewGuid();

        var result = await service.CreateAsync(request, createdBy.ToString());

        // Verify user was activated
        var updatedUser = await context.ApplicationUsers.FindAsync(pendingUser.Id);
        updatedUser!.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task CreateAsync_WithNewUserEmail_CreatesPendingUser()
    {
        var context = CreateTestContext();
        var service = CreateService(context);
        var request = new CreateOrganizationRequest(
            "New Org",
            "new-org",
            null,
            null,
            "newuser@test.com"
        );
        var createdBy = Guid.NewGuid();

        var result = await service.CreateAsync(request, createdBy.ToString());

        // Verify new user was created
        var newUser = context.ApplicationUsers.FirstOrDefault(u => u.Email == "newuser@test.com");
        newUser.Should().NotBeNull();
        newUser!.Status.Should().Be(UserStatus.Pending);

        // Verify membership was created
        var membership = context.OrganizationMemberships
            .FirstOrDefault(m => m.OrganizationId == result.Id && m.UserId == newUser.Id!);
        membership.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsInvalidOperationException()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Existing Org", "test-slug");
        var service = CreateService(context);
        var request = new CreateOrganizationRequest(
            "New Org",
            "test-slug",
            null,
            null,
            "admin@test.com"
        );
        var createdBy = Guid.NewGuid();

        var act = async () => await service.CreateAsync(request, createdBy.ToString());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*slug*already*");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesOrg()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Original Name", "original-slug");
        var service = CreateService(context);
        var request = new UpdateOrganizationRequest(
            "Updated Name",
            "Updated description",
            "updated@test.com"
        );

        var result = await service.UpdateAsync(org.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.ContactEmail.Should().Be("updated@test.com");
        result.Slug.Should().Be("original-slug"); // Slug unchanged
    }

    [Fact]
    public async Task UpdateAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);
        var request = new UpdateOrganizationRequest("Updated", null, null);

        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        result.Should().BeNull();
    }

    #endregion

    #region ArchiveAsync Tests

    [Fact]
    public async Task ArchiveAsync_ActiveOrg_SetsStatusToArchived()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Active Org", "active", OrgStatus.Active);
        var service = CreateService(context);
        var archivedBy = Guid.NewGuid();

        var result = await service.ArchiveAsync(org.Id, archivedBy.ToString());

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrgStatus.Archived.ToString());
    }

    [Fact]
    public async Task ArchiveAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = await service.ArchiveAsync(Guid.NewGuid(), Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_ActiveOrg_SetsStatusToInactive()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Active Org", "active", OrgStatus.Active);
        var service = CreateService(context);
        var deactivatedBy = Guid.NewGuid();

        var result = await service.DeactivateAsync(org.Id, deactivatedBy.ToString());

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrgStatus.Inactive.ToString());
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = await service.DeactivateAsync(Guid.NewGuid(), Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    #endregion

    #region RestoreAsync Tests

    [Fact]
    public async Task RestoreAsync_ArchivedOrg_SetsStatusToActive()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Archived Org", "archived", OrgStatus.Archived);
        var service = CreateService(context);
        var restoredBy = Guid.NewGuid().ToString();

        var result = await service.RestoreAsync(org.Id, restoredBy);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrgStatus.Active.ToString());
    }

    [Fact]
    public async Task RestoreAsync_InactiveOrg_SetsStatusToActive()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Inactive Org", "inactive", OrgStatus.Inactive);
        var service = CreateService(context);
        var restoredBy = Guid.NewGuid().ToString();

        var result = await service.RestoreAsync(org.Id, restoredBy);

        result.Should().NotBeNull();
        result!.Status.Should().Be(OrgStatus.Active.ToString());
    }

    [Fact]
    public async Task RestoreAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = await service.RestoreAsync(Guid.NewGuid(), Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    #endregion

    #region GenerateSlug Tests

    [Fact]
    public void GenerateSlug_ConvertsToLowercase()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = service.GenerateSlug("CISA Region 4");

        result.Should().Be("cisa-region-4");
    }

    [Fact]
    public void GenerateSlug_ReplacesSpacesWithHyphens()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = service.GenerateSlug("State Emergency Mgmt");

        result.Should().Be("state-emergency-mgmt");
    }

    [Fact]
    public void GenerateSlug_RemovesSpecialCharacters()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = service.GenerateSlug("Test & Demo Org!!!");

        result.Should().Be("test-demo-org");
    }

    [Fact]
    public void GenerateSlug_CollapsesMultipleHyphens()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = service.GenerateSlug("Test   Multiple   Spaces");

        result.Should().Be("test-multiple-spaces");
    }

    [Fact]
    public void GenerateSlug_TrimsHyphensFromStartAndEnd()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = service.GenerateSlug("  Test Org  ");

        result.Should().Be("test-org");
    }

    #endregion

    #region CheckSlugAsync Tests

    [Fact]
    public async Task CheckSlugAsync_AvailableSlug_ReturnsTrue()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Existing Org", "existing-slug");
        var service = CreateService(context);

        var result = await service.CheckSlugAsync("new-slug");

        result.Available.Should().BeTrue();
        result.Suggestion.Should().BeNull();
    }

    [Fact]
    public async Task CheckSlugAsync_TakenSlug_ReturnsFalseWithSuggestion()
    {
        var context = CreateTestContext();
        CreateOrganization(context, "Existing Org", "test-slug");
        var service = CreateService(context);

        var result = await service.CheckSlugAsync("test-slug");

        result.Available.Should().BeFalse();
        result.Suggestion.Should().NotBeNull();
        result.Suggestion.Should().StartWith("test-slug-");
    }

    [Fact]
    public async Task CheckSlugAsync_WithExcludeId_ExcludesSelf()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Existing Org", "test-slug");
        var service = CreateService(context);

        var result = await service.CheckSlugAsync("test-slug", excludeId: org.Id);

        result.Available.Should().BeTrue();
    }

    #endregion

    #region UpdateApprovalPolicyAsync Tests

    [Fact]
    public async Task UpdateApprovalPolicyAsync_WithValidPolicy_UpdatesPolicy()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test-org");
        org.InjectApprovalPolicy = ApprovalPolicy.Optional;
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.UpdateApprovalPolicyAsync(org.Id, ApprovalPolicy.Required);

        result.Should().NotBeNull();
        result!.Id.Should().Be(org.Id);

        // Verify the policy was updated in the database
        var updatedOrg = await context.Organizations.FindAsync(org.Id);
        updatedOrg!.InjectApprovalPolicy.Should().Be(ApprovalPolicy.Required);
    }

    [Fact]
    public async Task UpdateApprovalPolicyAsync_PolicyChangesFromOptionalToDisabled_UpdatesSuccessfully()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test-org");
        org.InjectApprovalPolicy = ApprovalPolicy.Optional;
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.UpdateApprovalPolicyAsync(org.Id, ApprovalPolicy.Disabled);

        result.Should().NotBeNull();
        var updatedOrg = await context.Organizations.FindAsync(org.Id);
        updatedOrg!.InjectApprovalPolicy.Should().Be(ApprovalPolicy.Disabled);
    }

    [Fact]
    public async Task UpdateApprovalPolicyAsync_PolicyChangesFromDisabledToRequired_UpdatesSuccessfully()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test-org");
        org.InjectApprovalPolicy = ApprovalPolicy.Disabled;
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.UpdateApprovalPolicyAsync(org.Id, ApprovalPolicy.Required);

        result.Should().NotBeNull();
        var updatedOrg = await context.Organizations.FindAsync(org.Id);
        updatedOrg!.InjectApprovalPolicy.Should().Be(ApprovalPolicy.Required);
    }

    [Fact]
    public async Task UpdateApprovalPolicyAsync_NonExistentId_ReturnsNull()
    {
        var context = CreateTestContext();
        var service = CreateService(context);

        var result = await service.UpdateApprovalPolicyAsync(Guid.NewGuid(), ApprovalPolicy.Required);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateApprovalPolicyAsync_DeletedOrganization_ReturnsNull()
    {
        var context = CreateTestContext();
        var org = CreateOrganization(context, "Test Org", "test-org");
        org.IsDeleted = true;
        context.SaveChanges();
        var service = CreateService(context);

        var result = await service.UpdateApprovalPolicyAsync(org.Id, ApprovalPolicy.Required);

        result.Should().BeNull();
    }

    #endregion
}
