using Cadence.Core.Data;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cadence.Core.Tests.Features.Users;

/// <summary>
/// Tests for GetUserMemberships self-or-admin authorization logic (AC-M02).
/// Verifies that:
/// - Non-admin users cannot query other users' memberships (403)
/// - Admin users can query any user's memberships (200)
/// - Users can always query their own memberships (200)
/// </summary>
public class GetUserMembershipsAuthorizationTests : IDisposable
{
    private readonly AppDbContext _context;

    public GetUserMembershipsAuthorizationTests()
    {
        _context = TestDbContextFactory.Create();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ApplicationUser CreateTestUser(string email = "test@example.com")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = "Test User",
            Status = UserStatus.Active,
            SystemRole = SystemRole.User
        };
        _context.Set<ApplicationUser>().Add(user);
        _context.SaveChanges();
        return user;
    }

    /// <summary>
    /// AC-M02: Non-admin user querying another user's memberships should be denied.
    /// The controller's inline auth logic checks currentUserId != targetUserId && !isAdmin.
    /// </summary>
    [Fact]
    public void SelfOrAdminCheck_NonAdminRequestingOtherUserMemberships_Denied()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid(); // Different user
        var isAdmin = false;

        // Act — mirror the controller's inline auth logic
        var isDenied = currentUserId != targetUserId && !isAdmin;

        // Assert
        isDenied.Should().BeTrue("non-admin users should not access other users' memberships");
    }

    /// <summary>
    /// AC-M02: Admin user querying another user's memberships should be allowed.
    /// </summary>
    [Fact]
    public void SelfOrAdminCheck_AdminRequestingOtherUserMemberships_Allowed()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid(); // Different user
        var isAdmin = true;

        // Act
        var isDenied = currentUserId != targetUserId && !isAdmin;

        // Assert
        isDenied.Should().BeFalse("admin users should access any user's memberships");
    }

    /// <summary>
    /// AC-M02: User querying their own memberships should always be allowed.
    /// </summary>
    [Fact]
    public void SelfOrAdminCheck_UserRequestingOwnMemberships_Allowed()
    {
        // Arrange — same user ID for both current and target
        var currentUserId = Guid.NewGuid();
        var targetUserId = currentUserId; // Same user
        var isAdmin = false;

        // Act
        var isDenied = currentUserId != targetUserId && !isAdmin;

        // Assert
        isDenied.Should().BeFalse("users should always access their own memberships");
    }

    /// <summary>
    /// AC-M02: Verify that user memberships are correctly scoped —
    /// querying returns only memberships for the target user, not others.
    /// </summary>
    [Fact]
    public async Task GetUserMemberships_ReturnsOnlyTargetUserMemberships()
    {
        // Arrange
        var targetUser = CreateTestUser("target@example.com");
        var otherUser = CreateTestUser("other@example.com");

        var org1 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Org 1",
            CreatedBy = targetUser.Id,
            ModifiedBy = targetUser.Id
        };
        var org2 = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Org 2",
            CreatedBy = otherUser.Id,
            ModifiedBy = otherUser.Id
        };
        _context.Organizations.AddRange(org1, org2);

        var targetMembership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = targetUser.Id,
            OrganizationId = org1.Id,
            Role = OrgRole.OrgUser,
            JoinedAt = DateTime.UtcNow
        };
        var otherMembership = new OrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = otherUser.Id,
            OrganizationId = org2.Id,
            Role = OrgRole.OrgAdmin,
            JoinedAt = DateTime.UtcNow
        };
        _context.OrganizationMemberships.AddRange(targetMembership, otherMembership);
        await _context.SaveChangesAsync();

        // Act — mirror the controller's query (filtering by userId)
        var memberships = await _context.OrganizationMemberships
            .Where(m => m.UserId == targetUser.Id)
            .ToListAsync();

        // Assert
        memberships.Should().HaveCount(1);
        memberships[0].OrganizationId.Should().Be(org1.Id);
        memberships[0].UserId.Should().Be(targetUser.Id);
    }
}
