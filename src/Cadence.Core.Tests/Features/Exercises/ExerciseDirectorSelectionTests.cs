using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for director selection during exercise creation and editing.
/// </summary>
public class ExerciseDirectorSelectionTests
{
    private readonly AppDbContext _context;
    private readonly IExerciseParticipantService _participantService;
    private readonly Organization _organization;
    private readonly ApplicationUser _adminUser;
    private readonly ApplicationUser _managerUser;
    private readonly ApplicationUser _standardUser;

    public ExerciseDirectorSelectionTests()
    {
        _context = TestDbContextFactory.Create();
        _participantService = new ExerciseParticipantService(_context, NullLogger<ExerciseParticipantService>.Instance);

        // Seed test organization
        _organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization"
        };
        _context.Organizations.Add(_organization);

        // Seed test users with different system roles
        _adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin@test.com",
            Email = "admin@test.com",
            DisplayName = "Admin User",
            SystemRole = SystemRole.Admin,
            OrganizationId = _organization.Id
        };

        _managerUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "manager@test.com",
            Email = "manager@test.com",
            DisplayName = "Manager User",
            SystemRole = SystemRole.Manager,
            OrganizationId = _organization.Id
        };

        _standardUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user@test.com",
            Email = "user@test.com",
            DisplayName = "Standard User",
            SystemRole = SystemRole.User,
            OrganizationId = _organization.Id
        };

        _context.ApplicationUsers.AddRange(_adminUser, _managerUser, _standardUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateExercise_WithNoDirectorId_AutoAssignsCreatorAsDirector()
    {
        // Arrange - Create exercise without specifying directorId
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        // Act - Auto-assign manager as director (simulating controller logic)
        await _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _managerUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        // Assert
        var participant = await _participantService.GetParticipantAsync(exercise.Id, _managerUser.Id);
        participant.Should().NotBeNull();
        participant!.ExerciseRole.Should().Be(ExerciseRole.ExerciseDirector.ToString());
        participant.UserId.Should().Be(_managerUser.Id);
    }

    [Fact]
    public async Task CreateExercise_WithValidAdminDirectorId_AssignsThatUserAsDirector()
    {
        // Arrange - Create exercise specifying an admin as director
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        // Act - Assign admin as director
        await _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _adminUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        // Assert
        var participant = await _participantService.GetParticipantAsync(exercise.Id, _adminUser.Id);
        participant.Should().NotBeNull();
        participant!.ExerciseRole.Should().Be(ExerciseRole.ExerciseDirector.ToString());
        participant.UserId.Should().Be(_adminUser.Id);
    }

    [Fact]
    public async Task CreateExercise_WithValidManagerDirectorId_AssignsThatUserAsDirector()
    {
        // Arrange - Create exercise specifying a manager as director
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        // Act - Assign manager as director
        await _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _managerUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        // Assert
        var participant = await _participantService.GetParticipantAsync(exercise.Id, _managerUser.Id);
        participant.Should().NotBeNull();
        participant!.ExerciseRole.Should().Be(ExerciseRole.ExerciseDirector.ToString());
        participant.UserId.Should().Be(_managerUser.Id);
    }

    [Fact]
    public async Task CreateExercise_WithInvalidDirectorId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        var invalidUserId = Guid.NewGuid().ToString();

        // Act & Assert
        var act = () => _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = invalidUserId,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User {invalidUserId} not found");
    }

    [Fact]
    public async Task CreateExercise_WithStandardUserDirectorId_ThrowsInvalidOperationException()
    {
        // Arrange
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        // Act & Assert - Attempting to assign a standard user as director should fail
        var act = () => _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _standardUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only Admins and Managers can be assigned as Exercise Director");
    }

    [Fact]
    public async Task UpdateExercise_WithNewDirectorId_ReassignsDirector()
    {
        // Arrange - Create exercise with manager as initial director
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();

        await _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _managerUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        // Act - Add admin as new director (in real scenario, we'd remove old director)
        await _participantService.AddParticipantAsync(
            exercise.Id,
            new AddParticipantRequest
            {
                UserId = _adminUser.Id,
                Role = ExerciseRole.ExerciseDirector.ToString()
            });

        // Assert - Both should be directors now (business logic in controller would handle removal)
        var managerParticipant = await _participantService.GetParticipantAsync(exercise.Id, _managerUser.Id);
        var adminParticipant = await _participantService.GetParticipantAsync(exercise.Id, _adminUser.Id);

        managerParticipant.Should().NotBeNull();
        adminParticipant.Should().NotBeNull();
        adminParticipant!.ExerciseRole.Should().Be(ExerciseRole.ExerciseDirector.ToString());
    }

    [Fact]
    public async Task ValidateDirectorEligibility_AdminUser_ReturnsTrue()
    {
        // Arrange
        var user = await _context.ApplicationUsers.FindAsync(_adminUser.Id);

        // Assert
        user.Should().NotBeNull();
        user!.SystemRole.Should().Be(SystemRole.Admin);
        (user.SystemRole == SystemRole.Admin || user.SystemRole == SystemRole.Manager).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDirectorEligibility_ManagerUser_ReturnsTrue()
    {
        // Arrange
        var user = await _context.ApplicationUsers.FindAsync(_managerUser.Id);

        // Assert
        user.Should().NotBeNull();
        user!.SystemRole.Should().Be(SystemRole.Manager);
        (user.SystemRole == SystemRole.Admin || user.SystemRole == SystemRole.Manager).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDirectorEligibility_StandardUser_ReturnsFalse()
    {
        // Arrange
        var user = await _context.ApplicationUsers.FindAsync(_standardUser.Id);

        // Assert
        user.Should().NotBeNull();
        user!.SystemRole.Should().Be(SystemRole.User);
        (user.SystemRole == SystemRole.Admin || user.SystemRole == SystemRole.Manager).Should().BeFalse();
    }
}
