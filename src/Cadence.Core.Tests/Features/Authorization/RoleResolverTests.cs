using Cadence.Core.Data;
using Cadence.Core.Features.Authorization.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Authorization;

/// <summary>
/// Tests for RoleResolver - Core authorization business logic.
/// Tests role hierarchy, system admin bypass, and exercise participant validation.
/// </summary>
public class RoleResolverTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly RoleResolver _sut;

    // Test data
    private readonly Guid _exerciseId = Guid.NewGuid();
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly string _adminUserId = Guid.NewGuid().ToString();
    private readonly string _directorUserId = Guid.NewGuid().ToString();
    private readonly string _controllerUserId = Guid.NewGuid().ToString();
    private readonly string _evaluatorUserId = Guid.NewGuid().ToString();
    private readonly string _observerUserId = Guid.NewGuid().ToString();
    private readonly string _unassignedUserId = Guid.NewGuid().ToString();

    public RoleResolverTests()
    {
        _context = TestDbContextFactory.Create();
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        // Default: OrgUser with correct org (no escalation)
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgUser);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);
        _sut = new RoleResolver(_context, _orgContextMock.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Create organization
        var org = new Organization
        {
            Id = _organizationId,
            Name = "Test Organization"
        };
        _context.Organizations.Add(org);

        // Create users with different system roles
        var adminUser = new ApplicationUser
        {
            Id = _adminUserId,
            Email = "admin@example.com",
            UserName = "admin@example.com",
            DisplayName = "System Admin",
            SystemRole = SystemRole.Admin,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        var directorUser = new ApplicationUser
        {
            Id = _directorUserId,
            Email = "director@example.com",
            UserName = "director@example.com",
            DisplayName = "Exercise Director",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        var controllerUser = new ApplicationUser
        {
            Id = _controllerUserId,
            Email = "controller@example.com",
            UserName = "controller@example.com",
            DisplayName = "Controller",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        var evaluatorUser = new ApplicationUser
        {
            Id = _evaluatorUserId,
            Email = "evaluator@example.com",
            UserName = "evaluator@example.com",
            DisplayName = "Evaluator",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        var observerUser = new ApplicationUser
        {
            Id = _observerUserId,
            Email = "observer@example.com",
            UserName = "observer@example.com",
            DisplayName = "Observer",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        var unassignedUser = new ApplicationUser
        {
            Id = _unassignedUserId,
            Email = "unassigned@example.com",
            UserName = "unassigned@example.com",
            DisplayName = "Unassigned User",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = org.Id
        };

        _context.ApplicationUsers.AddRange(
            adminUser,
            directorUser,
            controllerUser,
            evaluatorUser,
            observerUser,
            unassignedUser
        );

        // Create exercise
        var exercise = new Exercise
        {
            Id = _exerciseId,
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            OrganizationId = org.Id
        };
        _context.Exercises.Add(exercise);

        // Create exercise participants (admin is NOT assigned - tests bypass)
        var participants = new[]
        {
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = _directorUserId,
                Role = ExerciseRole.ExerciseDirector
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = _controllerUserId,
                Role = ExerciseRole.Controller
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = _evaluatorUserId,
                Role = ExerciseRole.Evaluator
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = _observerUserId,
                Role = ExerciseRole.Observer
            }
        };

        _context.ExerciseParticipants.AddRange(participants);
        _context.SaveChanges();
    }

    // =========================================================================
    // GetExerciseRoleAsync Tests
    // =========================================================================

    #region GetExerciseRoleAsync Tests

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedParticipant_ReturnsCorrectRole()
    {
        // Act
        var role = await _sut.GetExerciseRoleAsync(_controllerUserId, _exerciseId);

        // Assert
        role.Should().Be(ExerciseRole.Controller);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_UnassignedUser_ReturnsNull()
    {
        // Act
        var role = await _sut.GetExerciseRoleAsync(_unassignedUserId, _exerciseId);

        // Assert
        role.Should().BeNull();
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AdminUser_ReturnsNull()
    {
        // Arrange - Admin is not assigned as participant

        // Act
        var role = await _sut.GetExerciseRoleAsync(_adminUserId, _exerciseId);

        // Assert - Admin has no explicit exercise role
        role.Should().BeNull();
    }

    [Fact]
    public async Task GetExerciseRoleAsync_NonExistentExercise_ReturnsNull()
    {
        // Act
        var role = await _sut.GetExerciseRoleAsync(_controllerUserId, Guid.NewGuid());

        // Assert
        role.Should().BeNull();
    }

    [Fact]
    public async Task GetExerciseRoleAsync_SoftDeletedParticipant_ReturnsNull()
    {
        // Arrange - Soft delete the controller participant
        var participant = _context.ExerciseParticipants
            .First(p => p.UserId == _controllerUserId && p.ExerciseId == _exerciseId);
        participant.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var role = await _sut.GetExerciseRoleAsync(_controllerUserId, _exerciseId);

        // Assert
        role.Should().BeNull();
    }

    #endregion

    // =========================================================================
    // CanAccessExerciseAsync Tests
    // =========================================================================

    #region CanAccessExerciseAsync Tests

    [Fact]
    public async Task CanAccessExerciseAsync_AdminUser_ReturnsTrue()
    {
        // Act
        var canAccess = await _sut.CanAccessExerciseAsync(_adminUserId, _exerciseId);

        // Assert
        canAccess.Should().BeTrue("System Admins can access all exercises");
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AssignedParticipant_ReturnsTrue()
    {
        // Act
        var canAccess = await _sut.CanAccessExerciseAsync(_observerUserId, _exerciseId);

        // Assert
        canAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessExerciseAsync_UnassignedUser_ReturnsFalse()
    {
        // Act
        var canAccess = await _sut.CanAccessExerciseAsync(_unassignedUserId, _exerciseId);

        // Assert
        canAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessExerciseAsync_NonExistentUser_ReturnsFalse()
    {
        // Act
        var canAccess = await _sut.CanAccessExerciseAsync(Guid.NewGuid().ToString(), _exerciseId);

        // Assert
        canAccess.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessExerciseAsync_SoftDeletedParticipant_ReturnsFalse()
    {
        // Arrange - Soft delete the observer participant
        var participant = _context.ExerciseParticipants
            .First(p => p.UserId == _observerUserId && p.ExerciseId == _exerciseId);
        participant.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var canAccess = await _sut.CanAccessExerciseAsync(_observerUserId, _exerciseId);

        // Assert
        canAccess.Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // HasExerciseRoleAsync Tests - Role Hierarchy
    // =========================================================================

    #region HasExerciseRoleAsync Tests

    [Theory]
    [InlineData(ExerciseRole.Observer, true)]
    [InlineData(ExerciseRole.Evaluator, true)]
    [InlineData(ExerciseRole.Controller, true)]
    [InlineData(ExerciseRole.ExerciseDirector, true)]
    [InlineData(ExerciseRole.Administrator, true)]
    public async Task HasExerciseRoleAsync_AdminUser_AlwaysReturnsTrue(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_adminUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected, "System Admins have administrator-equivalent access");
    }

    [Theory]
    [InlineData(ExerciseRole.Observer, false)]
    [InlineData(ExerciseRole.Evaluator, false)]
    [InlineData(ExerciseRole.Controller, false)]
    [InlineData(ExerciseRole.ExerciseDirector, false)]
    [InlineData(ExerciseRole.Administrator, false)]
    public async Task HasExerciseRoleAsync_UnassignedUser_AlwaysReturnsFalse(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_unassignedUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(ExerciseRole.Observer, true)]  // Observer >= Observer
    [InlineData(ExerciseRole.Evaluator, false)] // Observer < Evaluator
    [InlineData(ExerciseRole.Controller, false)] // Observer < Controller
    [InlineData(ExerciseRole.ExerciseDirector, false)] // Observer < Director
    [InlineData(ExerciseRole.Administrator, false)] // Observer < Administrator
    public async Task HasExerciseRoleAsync_ObserverUser_MatchesHierarchy(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_observerUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(ExerciseRole.Observer, true)]  // Evaluator >= Observer
    [InlineData(ExerciseRole.Evaluator, true)] // Evaluator >= Evaluator
    [InlineData(ExerciseRole.Controller, false)] // Evaluator < Controller
    [InlineData(ExerciseRole.ExerciseDirector, false)] // Evaluator < Director
    [InlineData(ExerciseRole.Administrator, false)] // Evaluator < Administrator
    public async Task HasExerciseRoleAsync_EvaluatorUser_MatchesHierarchy(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(ExerciseRole.Observer, true)]  // Controller >= Observer
    [InlineData(ExerciseRole.Evaluator, true)] // Controller >= Evaluator
    [InlineData(ExerciseRole.Controller, true)] // Controller >= Controller
    [InlineData(ExerciseRole.ExerciseDirector, false)] // Controller < Director
    [InlineData(ExerciseRole.Administrator, false)] // Controller < Administrator
    public async Task HasExerciseRoleAsync_ControllerUser_MatchesHierarchy(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_controllerUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(ExerciseRole.Observer, true)]  // Director >= Observer
    [InlineData(ExerciseRole.Evaluator, true)] // Director >= Evaluator
    [InlineData(ExerciseRole.Controller, true)] // Director >= Controller
    [InlineData(ExerciseRole.ExerciseDirector, true)] // Director >= Director
    [InlineData(ExerciseRole.Administrator, false)] // Director < Administrator
    public async Task HasExerciseRoleAsync_DirectorUser_MatchesHierarchy(ExerciseRole minimumRole, bool expected)
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_directorUserId, _exerciseId, minimumRole);

        // Assert
        hasRole.Should().Be(expected);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_NonExistentUser_ReturnsFalse()
    {
        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(Guid.NewGuid().ToString(), _exerciseId, ExerciseRole.Observer);

        // Assert
        hasRole.Should().BeFalse();
    }

    [Fact]
    public async Task HasExerciseRoleAsync_SoftDeletedParticipant_ReturnsFalse()
    {
        // Arrange - Soft delete the controller participant
        var participant = _context.ExerciseParticipants
            .First(p => p.UserId == _controllerUserId && p.ExerciseId == _exerciseId);
        participant.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var hasRole = await _sut.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller);

        // Assert
        hasRole.Should().BeFalse();
    }

    #endregion

    // =========================================================================
    // GetSystemRoleAsync Tests
    // =========================================================================

    #region GetSystemRoleAsync Tests

    [Fact]
    public async Task GetSystemRoleAsync_AdminUser_ReturnsAdmin()
    {
        // Act
        var systemRole = await _sut.GetSystemRoleAsync(_adminUserId);

        // Assert
        systemRole.Should().Be(SystemRole.Admin);
    }

    [Fact]
    public async Task GetSystemRoleAsync_RegularUser_ReturnsUser()
    {
        // Act
        var systemRole = await _sut.GetSystemRoleAsync(_controllerUserId);

        // Assert
        systemRole.Should().Be(SystemRole.User);
    }

    [Fact]
    public async Task GetSystemRoleAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var systemRole = await _sut.GetSystemRoleAsync(Guid.NewGuid().ToString());

        // Assert
        systemRole.Should().BeNull();
    }

    #endregion

    // =========================================================================
    // Integration Scenario Tests
    // =========================================================================

    #region Integration Scenario Tests

    [Fact]
    public async Task Scenario_ObserverCannotFireInjects()
    {
        // Observer should have access but not Controller role
        var canAccess = await _sut.CanAccessExerciseAsync(_observerUserId, _exerciseId);
        var canFire = await _sut.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Controller);

        canAccess.Should().BeTrue("Observer is assigned to exercise");
        canFire.Should().BeFalse("Observer does not have Controller role");
    }

    [Fact]
    public async Task Scenario_ControllerCanFireInjects()
    {
        // Controller should have access and Controller role
        var canAccess = await _sut.CanAccessExerciseAsync(_controllerUserId, _exerciseId);
        var canFire = await _sut.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller);

        canAccess.Should().BeTrue("Controller is assigned to exercise");
        canFire.Should().BeTrue("Controller has Controller role");
    }

    [Fact]
    public async Task Scenario_EvaluatorCanCreateObservations()
    {
        // Evaluator should have access and Evaluator role
        var canAccess = await _sut.CanAccessExerciseAsync(_evaluatorUserId, _exerciseId);
        var canObserve = await _sut.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Evaluator);

        canAccess.Should().BeTrue("Evaluator is assigned to exercise");
        canObserve.Should().BeTrue("Evaluator has Evaluator role");
    }

    [Fact]
    public async Task Scenario_DirectorHasAllPermissions()
    {
        // Director should have all permissions except Administrator
        var canAccess = await _sut.CanAccessExerciseAsync(_directorUserId, _exerciseId);
        var canControl = await _sut.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Controller);
        var canEvaluate = await _sut.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Evaluator);
        var canDirect = await _sut.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.ExerciseDirector);
        var isAdmin = await _sut.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Administrator);

        canAccess.Should().BeTrue();
        canControl.Should().BeTrue("Director role includes Controller permissions");
        canEvaluate.Should().BeTrue("Director role includes Evaluator permissions");
        canDirect.Should().BeTrue("Director has Director role");
        isAdmin.Should().BeFalse("Director is not Administrator level");
    }

    [Fact]
    public async Task Scenario_AdminBypassesExerciseAssignment()
    {
        // Admin should have full access without being assigned
        var isAssigned = await _context.ExerciseParticipants
            .AnyAsync(p => p.UserId == _adminUserId && p.ExerciseId == _exerciseId && !p.IsDeleted);
        var canAccess = await _sut.CanAccessExerciseAsync(_adminUserId, _exerciseId);
        var hasFullAccess = await _sut.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Administrator);

        isAssigned.Should().BeFalse("Admin is not explicitly assigned");
        canAccess.Should().BeTrue("Admin can access all exercises");
        hasFullAccess.Should().BeTrue("Admin has administrator-equivalent permissions");
    }

    [Fact]
    public async Task Scenario_UnassignedUserDenied()
    {
        // Unassigned user should have no access
        var canAccess = await _sut.CanAccessExerciseAsync(_unassignedUserId, _exerciseId);
        var hasRole = await _sut.HasExerciseRoleAsync(_unassignedUserId, _exerciseId, ExerciseRole.Observer);

        canAccess.Should().BeFalse("User is not assigned to exercise");
        hasRole.Should().BeFalse("User has no role in exercise");
    }

    #endregion

    // =========================================================================
    // Org Role Escalation Tests
    // =========================================================================

    #region Org Role Escalation Tests

    [Fact]
    public async Task HasExerciseRoleAsync_OrgAdmin_EscalatesToExerciseDirector()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);

        // Observer assigned, but OrgAdmin should escalate to ExerciseDirector
        var hasDirectorRole = await _sut.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        hasDirectorRole.Should().BeTrue("OrgAdmin should escalate to ExerciseDirector-equivalent access");
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgManager_EscalatesToExerciseDirector()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgManager);

        var hasDirectorRole = await _sut.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        hasDirectorRole.Should().BeTrue("OrgManager should escalate to ExerciseDirector-equivalent access");
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgUser_NoEscalation()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgUser);

        var hasDirectorRole = await _sut.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        hasDirectorRole.Should().BeFalse("OrgUser should not get escalated permissions");
    }

    [Fact]
    public async Task CanAccessExerciseAsync_OrgAdmin_CanAccessOrgExercise()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        var canAccess = await _sut.CanAccessExerciseAsync(_unassignedUserId, _exerciseId);

        canAccess.Should().BeTrue("OrgAdmin should access exercises in their org");
    }

    [Fact]
    public async Task CanAccessExerciseAsync_OrgAdmin_CannotAccessOtherOrgExercise()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(Guid.NewGuid()); // Different org

        var canAccess = await _sut.CanAccessExerciseAsync(_unassignedUserId, _exerciseId);

        canAccess.Should().BeFalse("OrgAdmin should not access exercises in other orgs");
    }

    #endregion
}
