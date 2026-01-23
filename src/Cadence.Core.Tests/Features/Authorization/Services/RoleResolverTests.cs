using Cadence.Core.Data;
using Cadence.Core.Features.Authorization.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Tests.Features.Authorization.Services;

public class RoleResolverTests
{
    private readonly AppDbContext _context;
    private readonly RoleResolver _resolver;
    private readonly string _adminUserId = "admin-user-id";
    private readonly string _managerUserId = "manager-user-id";
    private readonly string _regularUserId = "regular-user-id";
    private readonly Guid _exerciseId = Guid.NewGuid();

    public RoleResolverTests()
    {
        _context = TestDbContextFactory.Create();
        _resolver = new RoleResolver(_context);
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test users
        var adminUser = new ApplicationUser
        {
            Id = _adminUserId,
            UserName = "admin@test.com",
            Email = "admin@test.com",
            DisplayName = "Admin User",
            SystemRole = SystemRole.Admin,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        var managerUser = new ApplicationUser
        {
            Id = _managerUserId,
            UserName = "manager@test.com",
            Email = "manager@test.com",
            DisplayName = "Manager User",
            SystemRole = SystemRole.Manager,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        var regularUser = new ApplicationUser
        {
            Id = _regularUserId,
            UserName = "user@test.com",
            Email = "user@test.com",
            DisplayName = "Regular User",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        _context.ApplicationUsers.AddRange(adminUser, managerUser, regularUser);

        // Create test exercise
        var exercise = new Exercise
        {
            Id = _exerciseId,
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            OrganizationId = Guid.NewGuid(),
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Exercises.Add(exercise);

        // Assign regular user as Controller in the exercise
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = _exerciseId,
            UserId = _regularUserId,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ExerciseParticipants.Add(participant);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSystemRoleAsync_AdminUser_ReturnsAdmin()
    {
        // Act
        var result = await _resolver.GetSystemRoleAsync(_adminUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SystemRole.Admin, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_ManagerUser_ReturnsManager()
    {
        // Act
        var result = await _resolver.GetSystemRoleAsync(_managerUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SystemRole.Manager, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_RegularUser_ReturnsUser()
    {
        // Act
        var result = await _resolver.GetSystemRoleAsync(_regularUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SystemRole.User, result.Value);
    }

    [Fact]
    public async Task GetSystemRoleAsync_NonExistentUser_ReturnsNull()
    {
        // Act
        var result = await _resolver.GetSystemRoleAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_AssignedParticipant_ReturnsRole()
    {
        // Act
        var result = await _resolver.GetExerciseRoleAsync(_regularUserId, _exerciseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ExerciseRole.Controller, result.Value);
    }

    [Fact]
    public async Task GetExerciseRoleAsync_NotAssigned_ReturnsNull()
    {
        // Act
        var result = await _resolver.GetExerciseRoleAsync(_managerUserId, _exerciseId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AdminUser_ReturnsTrue()
    {
        // Admins can access all exercises regardless of assignment
        // Act
        var result = await _resolver.CanAccessExerciseAsync(_adminUserId, _exerciseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_AssignedParticipant_ReturnsTrue()
    {
        // Act
        var result = await _resolver.CanAccessExerciseAsync(_regularUserId, _exerciseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_NotAssignedNonAdmin_ReturnsFalse()
    {
        // Manager is not assigned to the exercise and is not an Admin
        // Act
        var result = await _resolver.CanAccessExerciseAsync(_managerUserId, _exerciseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessExerciseAsync_NonExistentUser_ReturnsFalse()
    {
        // Act
        var result = await _resolver.CanAccessExerciseAsync("non-existent-id", _exerciseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_AdminUser_AlwaysReturnsTrue()
    {
        // Admins have Administrator-equivalent access to all exercises
        // Act
        var resultObserver = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Observer);
        var resultController = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.Controller);
        var resultDirector = await _resolver.HasExerciseRoleAsync(_adminUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        // Assert
        Assert.True(resultObserver);
        Assert.True(resultController);
        Assert.True(resultDirector);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithHigherRole_ReturnsTrue()
    {
        // Regular user is a Controller, checking for Observer role
        // Act
        var result = await _resolver.HasExerciseRoleAsync(_regularUserId, _exerciseId, ExerciseRole.Observer);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithExactRole_ReturnsTrue()
    {
        // Regular user is a Controller, checking for Controller role
        // Act
        var result = await _resolver.HasExerciseRoleAsync(_regularUserId, _exerciseId, ExerciseRole.Controller);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_ParticipantWithLowerRole_ReturnsFalse()
    {
        // Regular user is a Controller, checking for Director role
        // Act
        var result = await _resolver.HasExerciseRoleAsync(_regularUserId, _exerciseId, ExerciseRole.ExerciseDirector);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_NotAssignedNonAdmin_ReturnsFalse()
    {
        // Manager is not assigned to the exercise
        // Act
        var result = await _resolver.HasExerciseRoleAsync(_managerUserId, _exerciseId, ExerciseRole.Observer);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasExerciseRoleAsync_RoleHierarchy_ValidatesCorrectly()
    {
        // Test complete role hierarchy: Observer < Evaluator < Controller < Director < Administrator
        // Create participants with different roles
        var observerId = "observer-id";
        var evaluatorId = "evaluator-id";
        var directorId = "director-id";

        var observer = new ApplicationUser
        {
            Id = observerId,
            UserName = "observer@test.com",
            Email = "observer@test.com",
            DisplayName = "Observer",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        var evaluator = new ApplicationUser
        {
            Id = evaluatorId,
            UserName = "evaluator@test.com",
            Email = "evaluator@test.com",
            DisplayName = "Evaluator",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        var director = new ApplicationUser
        {
            Id = directorId,
            UserName = "director@test.com",
            Email = "director@test.com",
            DisplayName = "Director",
            SystemRole = SystemRole.Manager,
            Status = UserStatus.Active,
            EmailConfirmed = true,
            OrganizationId = Guid.NewGuid()
        };

        _context.ApplicationUsers.AddRange(observer, evaluator, director);

        _context.ExerciseParticipants.AddRange(
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = observerId,
                Role = ExerciseRole.Observer,
                AssignedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = evaluatorId,
                Role = ExerciseRole.Evaluator,
                AssignedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = _exerciseId,
                UserId = directorId,
                Role = ExerciseRole.ExerciseDirector,
                AssignedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        _context.SaveChanges();

        // Act & Assert - Observer can only access Observer level
        Assert.True(await _resolver.HasExerciseRoleAsync(observerId, _exerciseId, ExerciseRole.Observer));
        Assert.False(await _resolver.HasExerciseRoleAsync(observerId, _exerciseId, ExerciseRole.Evaluator));
        Assert.False(await _resolver.HasExerciseRoleAsync(observerId, _exerciseId, ExerciseRole.Controller));

        // Act & Assert - Evaluator can access Observer and Evaluator
        Assert.True(await _resolver.HasExerciseRoleAsync(evaluatorId, _exerciseId, ExerciseRole.Observer));
        Assert.True(await _resolver.HasExerciseRoleAsync(evaluatorId, _exerciseId, ExerciseRole.Evaluator));
        Assert.False(await _resolver.HasExerciseRoleAsync(evaluatorId, _exerciseId, ExerciseRole.Controller));

        // Act & Assert - Director can access all below Director
        Assert.True(await _resolver.HasExerciseRoleAsync(directorId, _exerciseId, ExerciseRole.Observer));
        Assert.True(await _resolver.HasExerciseRoleAsync(directorId, _exerciseId, ExerciseRole.Evaluator));
        Assert.True(await _resolver.HasExerciseRoleAsync(directorId, _exerciseId, ExerciseRole.Controller));
        Assert.True(await _resolver.HasExerciseRoleAsync(directorId, _exerciseId, ExerciseRole.ExerciseDirector));
    }
}
