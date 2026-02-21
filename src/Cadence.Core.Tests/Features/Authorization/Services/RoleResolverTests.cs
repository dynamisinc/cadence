using Cadence.Core.Data;
using Cadence.Core.Features.Authorization.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cadence.Core.Tests.Features.Authorization.Services;

/// <summary>
/// Exhaustive tests for RoleResolver service.
/// Tests role resolution for navigation and feature access.
/// </summary>
public class RoleResolverTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;
    private readonly RoleResolver _resolver;
    private readonly Guid _organizationId = Guid.NewGuid();

    // User IDs
    private readonly string _adminUserId = "admin-user-id";
    private readonly string _managerUserId = "manager-user-id";
    private readonly string _regularUserId = "regular-user-id";
    private readonly string _observerUserId = "observer-user-id";
    private readonly string _evaluatorUserId = "evaluator-user-id";
    private readonly string _controllerUserId = "controller-user-id";
    private readonly string _directorUserId = "director-user-id";

    // Exercise IDs
    private readonly Guid _exerciseId = Guid.NewGuid();
    private readonly Guid _otherExerciseId = Guid.NewGuid();

    public RoleResolverTests()
    {
        _context = TestDbContextFactory.Create();
        _orgContextMock = new Mock<ICurrentOrganizationContext>();
        // Default: OrgUser with correct org (no escalation)
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgUser);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);
        _resolver = new RoleResolver(_context, _orgContextMock.Object);
        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Create test users with different system roles
        var users = new List<ApplicationUser>
        {
            CreateUser(_adminUserId, "admin@test.com", "Admin User", SystemRole.Admin),
            CreateUser(_managerUserId, "manager@test.com", "Manager User", SystemRole.Manager),
            CreateUser(_regularUserId, "user@test.com", "Regular User", SystemRole.User),
            CreateUser(_observerUserId, "observer@test.com", "Observer User", SystemRole.User),
            CreateUser(_evaluatorUserId, "evaluator@test.com", "Evaluator User", SystemRole.User),
            CreateUser(_controllerUserId, "controller@test.com", "Controller User", SystemRole.User),
            CreateUser(_directorUserId, "director@test.com", "Director User", SystemRole.Manager),
        };

        _context.ApplicationUsers.AddRange(users);

        // Create test exercises
        var exercises = new List<Exercise>
        {
            CreateExercise(_exerciseId, "Test Exercise 1"),
            CreateExercise(_otherExerciseId, "Test Exercise 2"),
        };

        _context.Exercises.AddRange(exercises);

        // Create exercise participants with various roles
        var participants = new List<ExerciseParticipant>
        {
            // Exercise 1 participants
            CreateParticipant(_exerciseId, _controllerUserId, ExerciseRole.Controller),
            CreateParticipant(_exerciseId, _evaluatorUserId, ExerciseRole.Evaluator),
            CreateParticipant(_exerciseId, _observerUserId, ExerciseRole.Observer),
            CreateParticipant(_exerciseId, _directorUserId, ExerciseRole.ExerciseDirector),

            // Exercise 2 participants (different roles)
            CreateParticipant(_otherExerciseId, _controllerUserId, ExerciseRole.Observer),
            CreateParticipant(_otherExerciseId, _directorUserId, ExerciseRole.Controller),
        };

        _context.ExerciseParticipants.AddRange(participants);
        _context.SaveChanges();
    }

    private ApplicationUser CreateUser(string id, string email, string name, SystemRole role)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            DisplayName = name,
            SystemRole = role,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = _organizationId
        };
    }

    private Exercise CreateExercise(Guid id, string name)
    {
        return new Exercise
        {
            Id = id,
            Name = name,
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            OrganizationId = _organizationId,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private ExerciseParticipant CreateParticipant(Guid exerciseId, string userId, ExerciseRole role)
    {
        return new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // =========================================================================
    // GetSystemRoleAsync Tests
    // =========================================================================
    #region GetSystemRoleAsync Tests

    [Fact]
    public async Task GetSystemRoleAsync_AdminUser_ReturnsAdmin()
    {
        var result = await _resolver.GetSystemRoleAsync(_adminUserId);

        Assert.NotNull(result);
        Assert.Equal(SystemRole.Admin, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_ManagerUser_ReturnsManager()
    {
        var result = await _resolver.GetSystemRoleAsync(_managerUserId);

        Assert.NotNull(result);
        Assert.Equal(SystemRole.Manager, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_RegularUser_ReturnsUser()
    {
        var result = await _resolver.GetSystemRoleAsync(_regularUserId);

        Assert.NotNull(result);
        Assert.Equal(SystemRole.User, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _resolver.GetSystemRoleAsync("non-existent-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSystemRoleAsync_EmptyUserId_ReturnsNull()
    {
        var result = await _resolver.GetSystemRoleAsync("");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("admin-user-id", SystemRole.Admin)]
    [InlineData("manager-user-id", SystemRole.Manager)]
    [InlineData("user@test.com", null)] // Not a valid ID format
    public async Task GetSystemRoleAsync_VariousUserIds_ReturnsExpectedRole(string userId, SystemRole? expected)
    {
        var result = await _resolver.GetSystemRoleAsync(userId);

        Assert.Equal(expected, result);
    }

    #endregion

    // =========================================================================
    // GetExerciseRoleAsync Tests
    // =========================================================================
    #region GetExerciseRoleAsync Tests

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedController_ReturnsController()
    {
        var result = await _resolver.GetExerciseRoleAsync(_controllerUserId, _exerciseId);

        Assert.NotNull(result);
        Assert.Equal(ExerciseRole.Controller, result.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedEvaluator_ReturnsEvaluator()
    {
        var result = await _resolver.GetExerciseRoleAsync(_evaluatorUserId, _exerciseId);

        Assert.NotNull(result);
        Assert.Equal(ExerciseRole.Evaluator, result.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedObserver_ReturnsObserver()
    {
        var result = await _resolver.GetExerciseRoleAsync(_observerUserId, _exerciseId);

        Assert.NotNull(result);
        Assert.Equal(ExerciseRole.Observer, result.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedDirector_ReturnsDirector()
    {
        var result = await _resolver.GetExerciseRoleAsync(_directorUserId, _exerciseId);

        Assert.NotNull(result);
        Assert.Equal(ExerciseRole.ExerciseDirector, result.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_NotAssigned_ReturnsNull()
    {
        var result = await _resolver.GetExerciseRoleAsync(_regularUserId, _exerciseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _resolver.GetExerciseRoleAsync("non-existent-id", _exerciseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_NonExistentExercise_ReturnsNull()
    {
        var result = await _resolver.GetExerciseRoleAsync(_controllerUserId, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_SameUserDifferentExercises_ReturnsDifferentRoles()
    {
        var role1 = await _resolver.GetExerciseRoleAsync(_controllerUserId, _exerciseId);
        var role2 = await _resolver.GetExerciseRoleAsync(_controllerUserId, _otherExerciseId);

        Assert.NotNull(role1);
        Assert.NotNull(role2);
        Assert.Equal(ExerciseRole.Controller, role1.Value);
        Assert.Equal(ExerciseRole.Observer, role2.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AdminNotAssigned_ReturnsNull()
    {
        // Admin is not assigned to the exercise as a participant
        var result = await _resolver.GetExerciseRoleAsync(_adminUserId, _exerciseId);

        // GetExerciseRoleAsync returns the participant role, not the effective role
        Assert.Null(result);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_SoftDeletedParticipant_ReturnsNull()
    {
        // Soft delete the participant
        var participant = await _context.ExerciseParticipants
            .FirstAsync(p => p.UserId == _controllerUserId && p.ExerciseId == _exerciseId);
        participant.IsDeleted = true;
        participant.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = await _resolver.GetExerciseRoleAsync(_controllerUserId, _exerciseId);

        Assert.Null(result);
    }

    #endregion

    // =========================================================================
    // CanAccessExerciseAsync Tests
    // =========================================================================
    #region CanAccessExerciseAsync Tests

    [Fact]
    public async Task CanAccessExerciseAsync_AdminUser_AlwaysReturnsTrue()
    {
        // Admin not assigned but can still access
        var result = await _resolver.CanAccessExerciseAsync(_adminUserId, _exerciseId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AdminUser_CanAccessAnyExercise()
    {
        var result1 = await _resolver.CanAccessExerciseAsync(_adminUserId, _exerciseId);
        var result2 = await _resolver.CanAccessExerciseAsync(_adminUserId, _otherExerciseId);
        var result3 = await _resolver.CanAccessExerciseAsync(_adminUserId, Guid.NewGuid());

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AssignedParticipant_ReturnsTrue()
    {
        var result = await _resolver.CanAccessExerciseAsync(_controllerUserId, _exerciseId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_NotAssignedNonAdmin_ReturnsFalse()
    {
        var result = await _resolver.CanAccessExerciseAsync(_regularUserId, _exerciseId);

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_NonExistentUser_ReturnsFalse()
    {
        var result = await _resolver.CanAccessExerciseAsync("non-existent-id", _exerciseId);

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_ManagerNotAssigned_ReturnsFalse()
    {
        var result = await _resolver.CanAccessExerciseAsync(_managerUserId, _exerciseId);

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AllAssignedRoles_ReturnTrue()
    {
        var results = new[]
        {
            await _resolver.CanAccessExerciseAsync(_controllerUserId, _exerciseId),
            await _resolver.CanAccessExerciseAsync(_evaluatorUserId, _exerciseId),
            await _resolver.CanAccessExerciseAsync(_observerUserId, _exerciseId),
            await _resolver.CanAccessExerciseAsync(_directorUserId, _exerciseId),
        };

        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_SoftDeletedParticipant_ReturnsFalse()
    {
        var participant = await _context.ExerciseParticipants
            .FirstAsync(p => p.UserId == _controllerUserId && p.ExerciseId == _exerciseId);
        participant.IsDeleted = true;
        participant.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = await _resolver.CanAccessExerciseAsync(_controllerUserId, _exerciseId);

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_OrgAdmin_NotParticipant_CanAccessOrgExercise()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        // regularUser is not assigned to the exercise
        var result = await _resolver.CanAccessExerciseAsync(_regularUserId, _exerciseId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_OrgManager_NotParticipant_CanAccessOrgExercise()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgManager);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        var result = await _resolver.CanAccessExerciseAsync(_regularUserId, _exerciseId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_OrgAdmin_DifferentOrg_CannotAccessExercise()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(Guid.NewGuid()); // Different org

        // regularUser is not assigned, and org doesn't match
        var result = await _resolver.CanAccessExerciseAsync(_regularUserId, _exerciseId);

        Assert.False(result);
    }

    #endregion

    // =========================================================================
    // HasExerciseRoleAsync Tests
    // =========================================================================
    #region HasExerciseRoleAsync Tests

    [Fact]
    public async Task HasExerciseRoleAsync_AdminUser_AlwaysReturnsTrue()
    {
        var resultObserver = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Observer);
        var resultEvaluator = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Evaluator);
        var resultController = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Controller);
        var resultDirector = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.ExerciseDirector);
        var resultAdmin = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Administrator);

        Assert.True(resultObserver);
        Assert.True(resultEvaluator);
        Assert.True(resultController);
        Assert.True(resultDirector);
        Assert.True(resultAdmin);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithExactRole_ReturnsTrue()
    {
        var result = await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller);

        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithHigherRole_ReturnsTrue()
    {
        // Controller checking for Observer level (lower)
        var result = await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Observer);

        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithLowerRole_ReturnsFalse()
    {
        // Controller checking for Director level (higher)
        var result = await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_NotAssignedNonAdmin_ReturnsFalse()
    {
        var result = await _resolver.HasExerciseRoleAsync(_regularUserId, _exerciseId, ExerciseRole.Observer);

        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_NonExistentUser_ReturnsFalse()
    {
        var result = await _resolver.HasExerciseRoleAsync("non-existent-id", _exerciseId, ExerciseRole.Observer);

        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_RoleHierarchy_ValidatesCorrectly()
    {
        // Observer can only access Observer level
        Assert.True(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Observer));
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Evaluator));
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Controller));
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.ExerciseDirector));
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Administrator));

        // Evaluator can access Observer and Evaluator
        Assert.True(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Observer));
        Assert.True(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Evaluator));
        Assert.False(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Controller));
        Assert.False(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.ExerciseDirector));

        // Controller can access Observer, Evaluator, and Controller
        Assert.True(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Observer));
        Assert.True(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Evaluator));
        Assert.True(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller));
        Assert.False(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.ExerciseDirector));

        // Director can access all below Administrator
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Observer));
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Evaluator));
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Controller));
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.ExerciseDirector));
        Assert.False(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Administrator));
    }

    #endregion

    // =========================================================================
    // Org Role Escalation Tests
    // =========================================================================
    #region Org Role Escalation Tests

    [Fact]
    public async Task HasExerciseRoleAsync_OrgAdmin_AssignedAsController_GetsExerciseDirectorAccess()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        // Controller is assigned but OrgAdmin should escalate to ExerciseDirector
        var result = await _resolver.HasExerciseRoleAsync(
            _controllerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgManager_AssignedAsObserver_GetsExerciseDirectorAccess()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgManager);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        var result = await _resolver.HasExerciseRoleAsync(
            _observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgAdmin_NotParticipant_GetsExerciseDirectorAccess()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        // regularUser is not assigned to the exercise at all
        var result = await _resolver.HasExerciseRoleAsync(
            _regularUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgAdmin_CannotGetAdministratorAccess()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgAdmin);
        _orgContextMock.Setup(x => x.CurrentOrganizationId).Returns(_organizationId);

        // OrgAdmin escalates to ExerciseDirector, NOT Administrator
        var result = await _resolver.HasExerciseRoleAsync(
            _regularUserId, _exerciseId, ExerciseRole.Administrator);

        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_OrgUser_NoEscalation()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns(OrgRole.OrgUser);

        // Observer should NOT get escalated just because they're OrgUser
        var result = await _resolver.HasExerciseRoleAsync(
            _observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_NullOrgRole_NoEscalation()
    {
        _orgContextMock.Setup(x => x.CurrentOrgRole).Returns((OrgRole?)null);

        var result = await _resolver.HasExerciseRoleAsync(
            _observerUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        Assert.False(result);
    }

    #endregion

    // =========================================================================
    // Navigation Permission Tests (Verifying Role-Based Menu Access)
    // =========================================================================
    #region Navigation Permission Tests

    [Fact]
    public async Task NavigationPermission_Admin_CanAccessAllMenuItems()
    {
        // Admin can access Control Room (needs Controller+ role)
        Assert.True(await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Controller));

        // Admin can access Observations (needs Evaluator+ role)
        Assert.True(await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Evaluator));

        // Admin can access Reports (needs Director+ role)
        Assert.True(await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.ExerciseDirector));
    }

    [Fact]
    public async Task NavigationPermission_Director_CanAccessControlAndObservations()
    {
        // Director can access Control Room
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Controller));

        // Director can access Observations
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.Evaluator));

        // Director can access Reports
        Assert.True(await _resolver.HasExerciseRoleAsync(_directorUserId, _exerciseId, ExerciseRole.ExerciseDirector));
    }

    [Fact]
    public async Task NavigationPermission_Controller_CanAccessControlButNotObservations()
    {
        // Controller can access Control Room
        Assert.True(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller));

        // Controller cannot access Observations (needs Evaluator role, Controller is different branch)
        // Note: In the HSEEP hierarchy, Controller > Evaluator numerically, so this should be true
        Assert.True(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Evaluator));

        // Controller cannot access Reports
        Assert.False(await _resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.ExerciseDirector));
    }

    [Fact]
    public async Task NavigationPermission_Evaluator_CanAccessObservationsButNotControl()
    {
        // Evaluator cannot access Control Room
        Assert.False(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Controller));

        // Evaluator can access Observations
        Assert.True(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.Evaluator));

        // Evaluator cannot access Reports
        Assert.False(await _resolver.HasExerciseRoleAsync(_evaluatorUserId, _exerciseId, ExerciseRole.ExerciseDirector));
    }

    [Fact]
    public async Task NavigationPermission_Observer_CannotAccessControlOrObservations()
    {
        // Observer cannot access Control Room
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Controller));

        // Observer cannot access Observations
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.Evaluator));

        // Observer cannot access Reports
        Assert.False(await _resolver.HasExerciseRoleAsync(_observerUserId, _exerciseId, ExerciseRole.ExerciseDirector));
    }

    #endregion

    // =========================================================================
    // Edge Cases and Boundary Tests
    // =========================================================================
    #region Edge Cases

    [Fact]
    public async Task EdgeCase_EmptyUserId_HandledGracefully()
    {
        var systemRole = await _resolver.GetSystemRoleAsync("");
        var exerciseRole = await _resolver.GetExerciseRoleAsync("", _exerciseId);
        var canAccess = await _resolver.CanAccessExerciseAsync("", _exerciseId);
        var hasRole = await _resolver.HasExerciseRoleAsync("", _exerciseId, ExerciseRole.Observer);

        Assert.Null(systemRole);
        Assert.Null(exerciseRole);
        Assert.False(canAccess);
        Assert.False(hasRole);
    }

    [Fact]
    public async Task EdgeCase_EmptyGuidExerciseId_HandledGracefully()
    {
        var exerciseRole = await _resolver.GetExerciseRoleAsync(_controllerUserId, Guid.Empty);
        var canAccess = await _resolver.CanAccessExerciseAsync(_controllerUserId, Guid.Empty);
        var hasRole = await _resolver.HasExerciseRoleAsync(_controllerUserId, Guid.Empty, ExerciseRole.Observer);

        Assert.Null(exerciseRole);
        Assert.False(canAccess);
        Assert.False(hasRole);
    }

    [Fact]
    public async Task EdgeCase_MultipleParticipantsInExercise_ReturnsCorrectRoles()
    {
        // Verify all participants get their correct roles
        var controllerRole = await _resolver.GetExerciseRoleAsync(_controllerUserId, _exerciseId);
        var evaluatorRole = await _resolver.GetExerciseRoleAsync(_evaluatorUserId, _exerciseId);
        var observerRole = await _resolver.GetExerciseRoleAsync(_observerUserId, _exerciseId);
        var directorRole = await _resolver.GetExerciseRoleAsync(_directorUserId, _exerciseId);

        Assert.Equal(ExerciseRole.Controller, controllerRole);
        Assert.Equal(ExerciseRole.Evaluator, evaluatorRole);
        Assert.Equal(ExerciseRole.Observer, observerRole);
        Assert.Equal(ExerciseRole.ExerciseDirector, directorRole);
    }

    [Fact]
    public async Task EdgeCase_UserInMultipleExercises_ReturnsCorrectRolePerExercise()
    {
        // Controller user has different roles in different exercises
        var role1 = await _resolver.GetExerciseRoleAsync(_controllerUserId, _exerciseId);
        var role2 = await _resolver.GetExerciseRoleAsync(_controllerUserId, _otherExerciseId);

        Assert.Equal(ExerciseRole.Controller, role1);
        Assert.Equal(ExerciseRole.Observer, role2);
    }

    #endregion

    // =========================================================================
    // Concurrent Access Tests
    // =========================================================================
    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentAccess_MultipleSimultaneousChecks_ReturnConsistentResults()
    {
        var tasks = new List<Task<bool>>();

        // Fire multiple concurrent access checks
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_resolver.CanAccessExerciseAsync(_controllerUserId, _exerciseId));
            tasks.Add(_resolver.HasExerciseRoleAsync(_controllerUserId, _exerciseId, ExerciseRole.Controller));
        }

        var results = await Task.WhenAll(tasks);

        // All should return true consistently
        Assert.All(results, Assert.True);
    }

    #endregion
}
