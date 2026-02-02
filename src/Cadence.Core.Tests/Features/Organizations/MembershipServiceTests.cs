using Cadence.Core.Data;
using Cadence.Core.Exceptions;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadence.Core.Tests.Features.Organizations;

/// <summary>
/// Tests for MembershipService - user-organization membership management.
/// </summary>
public class MembershipServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly MembershipService _sut;
    private readonly ApplicationUser _testUser;
    private readonly ApplicationUser _testAdmin;
    private readonly Organization _testOrg;
    private readonly Organization _testOrg2;

    public MembershipServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new MembershipService(_context, NullLogger<MembershipService>.Instance);

        // Seed test data
        _testOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            Status = OrgStatus.Active
        };

        _testOrg2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Second Organization",
            Slug = "second-org",
            Status = OrgStatus.Active
        };

        _testUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Pending,
            OrganizationId = _testOrg.Id
        };

        _testAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@example.com",
            DisplayName = "Admin User",
            Status = UserStatus.Active,
            SystemRole = SystemRole.Admin,
            OrganizationId = _testOrg.Id
        };

        _context.Organizations.AddRange(_testOrg, _testOrg2);
        _context.Set<ApplicationUser>().AddRange(_testUser, _testAdmin);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region AssignUserToOrganization Tests

    [Fact]
    public async Task AssignUserToOrg_CreatesNewMembership()
    {
        // Arrange
        var request = new AssignUserRequest(_testOrg.Id, OrgRole.OrgUser);

        // Act
        var result = await _sut.AssignUserToOrganizationAsync(_testUser.Id, request, _testAdmin.Id);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUser.Id);
        result.OrganizationId.Should().Be(_testOrg.Id);
        result.OrganizationName.Should().Be("Test Organization");
        result.OrganizationSlug.Should().Be("test-org");
        result.Role.Should().Be(OrgRole.OrgUser.ToString());

        var membershipInDb = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == _testUser.Id && m.OrganizationId == _testOrg.Id);
        membershipInDb.Should().NotBeNull();
        membershipInDb!.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task AssignUserToOrg_PendingUser_ChangesStatusToActive()
    {
        // Arrange
        _testUser.Status = UserStatus.Pending;
        await _context.SaveChangesAsync();

        var request = new AssignUserRequest(_testOrg.Id, OrgRole.OrgUser);

        // Act
        await _sut.AssignUserToOrganizationAsync(_testUser.Id, request, _testAdmin.Id);

        // Assert
        var user = await _context.Set<ApplicationUser>().FindAsync(_testUser.Id);
        user!.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task AssignUserToOrg_AlreadyActiveMember_ThrowsConflict()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        var request = new AssignUserRequest(_testOrg.Id, OrgRole.OrgAdmin);

        // Act & Assert
        await _sut.Invoking(s => s.AssignUserToOrganizationAsync(_testUser.Id, request, _testAdmin.Id))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*already has active membership*");
    }

    [Fact]
    public async Task AssignUserToOrg_InactiveMembership_ReactivatesIt()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Inactive
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        var request = new AssignUserRequest(_testOrg.Id, OrgRole.OrgManager);

        // Act
        var result = await _sut.AssignUserToOrganizationAsync(_testUser.Id, request, _testAdmin.Id);

        // Assert
        result.Should().NotBeNull();
        result.Role.Should().Be(OrgRole.OrgManager.ToString());

        var updatedMembership = await _context.OrganizationMemberships.FindAsync(membership.Id);
        updatedMembership!.Status.Should().Be(MembershipStatus.Active);
        updatedMembership.Role.Should().Be(OrgRole.OrgManager);
    }

    [Fact]
    public async Task AssignUserToOrg_UserNotFound_ThrowsNotFound()
    {
        // Arrange
        var request = new AssignUserRequest(_testOrg.Id, OrgRole.OrgUser);

        // Act & Assert
        await _sut.Invoking(s => s.AssignUserToOrganizationAsync("nonexistent-user-id", request, _testAdmin.Id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*User*not found*");
    }

    [Fact]
    public async Task AssignUserToOrg_OrganizationNotFound_ThrowsNotFound()
    {
        // Arrange
        var request = new AssignUserRequest(Guid.NewGuid(), OrgRole.OrgUser);

        // Act & Assert
        await _sut.Invoking(s => s.AssignUserToOrganizationAsync(_testUser.Id, request, _testAdmin.Id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Organization*not found*");
    }

    #endregion

    #region RemoveMembership Tests

    [Fact]
    public async Task RemoveFromOrg_LastOrg_ChangesStatusToPending()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        _testUser.Status = UserStatus.Active;
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveMembershipAsync(membership.Id, _testAdmin.Id);

        // Assert
        result.Removed.Should().BeTrue();
        result.UserStatusChanged.Should().BeTrue();
        result.NewUserStatus.Should().Be(UserStatus.Pending.ToString());

        var user = await _context.Set<ApplicationUser>().FindAsync(_testUser.Id);
        user!.Status.Should().Be(UserStatus.Pending);

        // Verify membership is soft-deleted (IsDeleted = true)
        var deletedMembership = await _context.OrganizationMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == membership.Id);
        deletedMembership.Should().NotBeNull();
        deletedMembership!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFromOrg_NotLastOrg_KeepsStatusActive()
    {
        // Arrange
        var membership1 = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        var membership2 = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg2.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.AddRange(membership1, membership2);
        _testUser.Status = UserStatus.Active;
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveMembershipAsync(membership1.Id, _testAdmin.Id);

        // Assert
        result.Removed.Should().BeTrue();
        result.UserStatusChanged.Should().BeFalse();

        var user = await _context.Set<ApplicationUser>().FindAsync(_testUser.Id);
        user!.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task RemoveFromOrg_OnlyOrgAdmin_ThrowsBusinessRule()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testAdmin.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgAdmin,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act & Assert
        await _sut.Invoking(s => s.RemoveMembershipAsync(membership.Id, _testAdmin.Id))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Cannot remove the last administrator*");
    }

    [Fact]
    public async Task RemoveFromOrg_MembershipNotFound_ThrowsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await _sut.Invoking(s => s.RemoveMembershipAsync(nonExistentId, _testAdmin.Id))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Membership*not found*");
    }

    #endregion

    #region UpdateMembershipRole Tests

    [Fact]
    public async Task ChangeRole_Valid_UpdatesRole()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        var request = new UpdateMembershipRequest(OrgRole.OrgManager);

        // Act
        var result = await _sut.UpdateMembershipRoleAsync(membership.Id, request);

        // Assert
        result.Role.Should().Be(OrgRole.OrgManager.ToString());

        var updatedMembership = await _context.OrganizationMemberships.FindAsync(membership.Id);
        updatedMembership!.Role.Should().Be(OrgRole.OrgManager);
    }

    [Fact]
    public async Task ChangeRole_ToNonAdmin_WhenOnlyAdmin_ThrowsBusinessRule()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testAdmin.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgAdmin,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        var request = new UpdateMembershipRequest(OrgRole.OrgUser);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateMembershipRoleAsync(membership.Id, request))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*at least one administrator*");
    }

    [Fact]
    public async Task ChangeRole_ToNonAdmin_WithOtherAdmins_Succeeds()
    {
        // Arrange
        var admin1Membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testAdmin.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgAdmin,
            Status = MembershipStatus.Active
        };

        var secondAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin2@example.com",
            DisplayName = "Second Admin",
            Status = UserStatus.Active,
            OrganizationId = _testOrg.Id
        };

        var admin2Membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = secondAdmin.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgAdmin,
            Status = MembershipStatus.Active
        };

        _context.Set<ApplicationUser>().Add(secondAdmin);
        _context.OrganizationMemberships.AddRange(admin1Membership, admin2Membership);
        await _context.SaveChangesAsync();

        var request = new UpdateMembershipRequest(OrgRole.OrgManager);

        // Act
        var result = await _sut.UpdateMembershipRoleAsync(admin1Membership.Id, request);

        // Assert
        result.Role.Should().Be(OrgRole.OrgManager.ToString());
    }

    [Fact]
    public async Task ChangeRole_MembershipNotFound_ThrowsNotFound()
    {
        // Arrange
        var request = new UpdateMembershipRequest(OrgRole.OrgManager);

        // Act & Assert
        await _sut.Invoking(s => s.UpdateMembershipRoleAsync(Guid.NewGuid(), request))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Membership*not found*");
    }

    #endregion

    #region GetUserMemberships Tests

    [Fact]
    public async Task GetUserMemberships_ReturnsAllActiveMemberships()
    {
        // Arrange
        var membership1 = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-10)
        };

        var membership2 = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg2.Id,
            Role = OrgRole.OrgManager,
            Status = MembershipStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.OrganizationMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetUserMembershipsAsync(_testUser.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.OrganizationId == _testOrg.Id && m.Role == OrgRole.OrgUser.ToString());
        result.Should().Contain(m => m.OrganizationId == _testOrg2.Id && m.Role == OrgRole.OrgManager.ToString());
    }

    [Fact]
    public async Task GetUserMemberships_NoMemberships_ReturnsEmpty()
    {
        // Act
        var result = await _sut.GetUserMembershipsAsync(_testUser.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region HasMembership Tests

    [Fact]
    public async Task HasMembership_WhenMember_ReturnsTrue()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.HasMembershipAsync(_testUser.Id, _testOrg.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMembership_WhenNotMember_ReturnsFalse()
    {
        // Act
        var result = await _sut.HasMembershipAsync(_testUser.Id, _testOrg.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMembership_InactiveMembership_ReturnsFalse()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgUser,
            Status = MembershipStatus.Inactive
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.HasMembershipAsync(_testUser.Id, _testOrg.Id);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetUserRoleInOrganization Tests

    [Fact]
    public async Task GetUserRoleInOrg_WhenMember_ReturnsRole()
    {
        // Arrange
        var membership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OrganizationId = _testOrg.Id,
            Role = OrgRole.OrgManager,
            Status = MembershipStatus.Active
        };
        _context.OrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserRoleInOrganizationAsync(_testUser.Id, _testOrg.Id);

        // Assert
        result.Should().Be(OrgRole.OrgManager);
    }

    [Fact]
    public async Task GetUserRoleInOrg_WhenNotMember_ReturnsNull()
    {
        // Act
        var result = await _sut.GetUserRoleInOrganizationAsync(_testUser.Id, _testOrg.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
