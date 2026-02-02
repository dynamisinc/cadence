using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for getting a user's exercise assignments (for profile menu).
/// </summary>
public class ExerciseAssignmentServiceTests
{
    private readonly AppDbContext _context;
    private readonly IExerciseParticipantService _participantService;
    private readonly Organization _organization;
    private readonly ApplicationUser _user1;
    private readonly ApplicationUser _user2;

    public ExerciseAssignmentServiceTests()
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

        // Seed test users
        _user1 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user1@test.com",
            Email = "user1@test.com",
            DisplayName = "User One",
            SystemRole = SystemRole.User,
            OrganizationId = _organization.Id
        };

        _user2 = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "user2@test.com",
            Email = "user2@test.com",
            DisplayName = "User Two",
            SystemRole = SystemRole.Manager,
            OrganizationId = _organization.Id
        };

        _context.ApplicationUsers.AddRange(_user1, _user2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetUserExerciseAssignments_UserWithMultipleAssignments_ReturnsAllAssignments()
    {
        // Arrange - Create multiple exercises with user1 as participant
        var exercise1 = CreateExercise("Hurricane Drill 2026");
        var exercise2 = CreateExercise("Earthquake Response");
        var exercise3 = CreateExercise("Flood Scenario");

        // Add user1 to all three exercises with different roles
        var participant1 = new ExerciseParticipant
        {
            ExerciseId = exercise1.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow.AddDays(-3)
        };
        var participant2 = new ExerciseParticipant
        {
            ExerciseId = exercise2.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Evaluator,
            AssignedAt = DateTime.UtcNow.AddDays(-2)
        };
        var participant3 = new ExerciseParticipant
        {
            ExerciseId = exercise3.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Observer,
            AssignedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.ExerciseParticipants.AddRange(participant1, participant2, participant3);
        await _context.SaveChangesAsync();

        // Act
        var assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);

        // Assert
        assignments.Should().HaveCount(3);

        var assignment1 = assignments.FirstOrDefault(a => a.ExerciseId == exercise1.Id);
        assignment1.Should().NotBeNull();
        assignment1!.ExerciseName.Should().Be("Hurricane Drill 2026");
        assignment1.ExerciseRole.Should().Be("Controller");

        var assignment2 = assignments.FirstOrDefault(a => a.ExerciseId == exercise2.Id);
        assignment2.Should().NotBeNull();
        assignment2!.ExerciseName.Should().Be("Earthquake Response");
        assignment2.ExerciseRole.Should().Be("Evaluator");

        var assignment3 = assignments.FirstOrDefault(a => a.ExerciseId == exercise3.Id);
        assignment3.Should().NotBeNull();
        assignment3!.ExerciseName.Should().Be("Flood Scenario");
        assignment3.ExerciseRole.Should().Be("Observer");
    }

    [Fact]
    public async Task GetUserExerciseAssignments_UserWithNoAssignments_ReturnsEmptyList()
    {
        // Arrange - user1 has no exercise assignments

        // Act
        var assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);

        // Assert
        assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserExerciseAssignments_ExcludesDeletedExercises()
    {
        // Arrange
        var activeExercise = CreateExercise("Active Exercise");
        var deletedExercise = CreateExercise("Deleted Exercise");
        deletedExercise.IsDeleted = true;
        deletedExercise.DeletedAt = DateTime.UtcNow;
        _context.Exercises.Update(deletedExercise);

        var participant1 = new ExerciseParticipant
        {
            ExerciseId = activeExercise.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        };
        var participant2 = new ExerciseParticipant
        {
            ExerciseId = deletedExercise.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        };

        _context.ExerciseParticipants.AddRange(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);

        // Assert
        assignments.Should().HaveCount(1);
        assignments.First().ExerciseName.Should().Be("Active Exercise");
    }

    [Fact]
    public async Task GetUserExerciseAssignments_ExcludesDeletedParticipants()
    {
        // Arrange
        var exercise1 = CreateExercise("Exercise 1");
        var exercise2 = CreateExercise("Exercise 2");

        var activeParticipant = new ExerciseParticipant
        {
            ExerciseId = exercise1.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        };
        var deletedParticipant = new ExerciseParticipant
        {
            ExerciseId = exercise2.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Evaluator,
            AssignedAt = DateTime.UtcNow,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        _context.ExerciseParticipants.AddRange(activeParticipant, deletedParticipant);
        await _context.SaveChangesAsync();

        // Act
        var assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);

        // Assert
        assignments.Should().HaveCount(1);
        assignments.First().ExerciseName.Should().Be("Exercise 1");
    }

    [Fact]
    public async Task GetUserExerciseAssignments_OrdersByAssignedAtDescending()
    {
        // Arrange - Create exercises with different assignment dates
        var exercise1 = CreateExercise("Oldest Assignment");
        var exercise2 = CreateExercise("Middle Assignment");
        var exercise3 = CreateExercise("Newest Assignment");

        var participant1 = new ExerciseParticipant
        {
            ExerciseId = exercise1.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Observer,
            AssignedAt = DateTime.UtcNow.AddDays(-10)
        };
        var participant2 = new ExerciseParticipant
        {
            ExerciseId = exercise2.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow.AddDays(-5)
        };
        var participant3 = new ExerciseParticipant
        {
            ExerciseId = exercise3.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Evaluator,
            AssignedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.ExerciseParticipants.AddRange(participant1, participant2, participant3);
        await _context.SaveChangesAsync();

        // Act
        var assignments = (await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id)).ToList();

        // Assert
        assignments.Should().HaveCount(3);
        assignments[0].ExerciseName.Should().Be("Newest Assignment");
        assignments[1].ExerciseName.Should().Be("Middle Assignment");
        assignments[2].ExerciseName.Should().Be("Oldest Assignment");
    }

    [Fact]
    public async Task GetUserExerciseAssignments_OnlyReturnsAssignmentsForSpecifiedUser()
    {
        // Arrange
        var exercise1 = CreateExercise("Exercise 1");
        var exercise2 = CreateExercise("Exercise 2");

        var participant1 = new ExerciseParticipant
        {
            ExerciseId = exercise1.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.Controller,
            AssignedAt = DateTime.UtcNow
        };
        var participant2 = new ExerciseParticipant
        {
            ExerciseId = exercise2.Id,
            UserId = _user2.Id,
            Role = ExerciseRole.Evaluator,
            AssignedAt = DateTime.UtcNow
        };

        _context.ExerciseParticipants.AddRange(participant1, participant2);
        await _context.SaveChangesAsync();

        // Act
        var user1Assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);
        var user2Assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user2.Id);

        // Assert
        user1Assignments.Should().HaveCount(1);
        user1Assignments.First().ExerciseName.Should().Be("Exercise 1");

        user2Assignments.Should().HaveCount(1);
        user2Assignments.First().ExerciseName.Should().Be("Exercise 2");
    }

    [Fact]
    public async Task GetUserExerciseAssignments_ReturnsCorrectDtoStructure()
    {
        // Arrange
        var exercise = CreateExercise("Test Exercise");
        var assignedAt = DateTime.UtcNow.AddHours(-2);

        var participant = new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = _user1.Id,
            Role = ExerciseRole.ExerciseDirector,
            AssignedAt = assignedAt
        };

        _context.ExerciseParticipants.Add(participant);
        await _context.SaveChangesAsync();

        // Act
        var assignments = await _participantService.GetUserExerciseAssignmentsAsync(_user1.Id);

        // Assert
        assignments.Should().HaveCount(1);
        var assignment = assignments.First();

        assignment.ExerciseId.Should().Be(exercise.Id);
        assignment.ExerciseName.Should().Be("Test Exercise");
        assignment.ExerciseRole.Should().Be("ExerciseDirector");
        assignment.AssignedAt.Should().BeCloseTo(assignedAt, TimeSpan.FromSeconds(1));
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private Exercise CreateExercise(string name)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            ExerciseType = ExerciseType.TTX,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = ExerciseStatus.Draft,
            OrganizationId = _organization.Id,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.Exercises.Add(exercise);
        _context.SaveChanges();

        return exercise;
    }
}
