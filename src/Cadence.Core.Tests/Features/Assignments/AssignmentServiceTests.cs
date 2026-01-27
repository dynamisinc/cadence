using Cadence.Core.Data;
using Cadence.Core.Features.Assignments.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Assignments;

/// <summary>
/// Tests for AssignmentService.
/// </summary>
public class AssignmentServiceTests
{
    private readonly AppDbContext _context;
    private readonly AssignmentService _service;
    private readonly Guid _testOrganizationId = Guid.NewGuid();
    private readonly string _testUserId = "test-user-id";

    public AssignmentServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<AssignmentService>>();
        _service = new AssignmentService(_context, logger.Object);
    }

    /// <summary>
    /// Helper to create a test exercise.
    /// </summary>
    private async Task<Exercise> CreateExerciseAsync(
        ExerciseStatus status = ExerciseStatus.Draft,
        string name = "Test Exercise")
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            OrganizationId = _testOrganizationId,
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            ExerciseType = ExerciseType.TTX
        };

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    /// <summary>
    /// Helper to create an exercise participant.
    /// </summary>
    private async Task<ExerciseParticipant> CreateParticipantAsync(
        Guid exerciseId,
        string userId,
        ExerciseRole role = ExerciseRole.Controller)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exerciseId,
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow
        };

        _context.ExerciseParticipants.Add(participant);
        await _context.SaveChangesAsync();
        return participant;
    }

    // =========================================================================
    // GetMyAssignmentsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetMyAssignmentsAsync_NoAssignments_ReturnsEmptyLists()
    {
        // Arrange - no assignments exist

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Active);
        Assert.Empty(result.Upcoming);
        Assert.Empty(result.Completed);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_WithActiveExercise_ReturnsInActiveSection()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Active, "Active Exercise");
        await CreateParticipantAsync(exercise.Id, _testUserId);

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Single(result.Active);
        Assert.Equal("Active Exercise", result.Active[0].ExerciseName);
        Assert.Empty(result.Upcoming);
        Assert.Empty(result.Completed);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_WithDraftExercise_ReturnsInUpcomingSection()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Draft, "Draft Exercise");
        await CreateParticipantAsync(exercise.Id, _testUserId);

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Empty(result.Active);
        Assert.Single(result.Upcoming);
        Assert.Equal("Draft Exercise", result.Upcoming[0].ExerciseName);
        Assert.Empty(result.Completed);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_WithCompletedExercise_ReturnsInCompletedSection()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Completed, "Completed Exercise");
        await CreateParticipantAsync(exercise.Id, _testUserId);

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Empty(result.Active);
        Assert.Empty(result.Upcoming);
        Assert.Single(result.Completed);
        Assert.Equal("Completed Exercise", result.Completed[0].ExerciseName);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_ExcludesDeletedExercises()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Active, "Deleted Exercise");
        exercise.IsDeleted = true;
        await _context.SaveChangesAsync();
        await CreateParticipantAsync(exercise.Id, _testUserId);

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Empty(result.Active);
        Assert.Empty(result.Upcoming);
        Assert.Empty(result.Completed);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_ExcludesOtherUsersAssignments()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Active, "Other User Exercise");
        await CreateParticipantAsync(exercise.Id, "other-user-id");

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Empty(result.Active);
    }

    [Fact]
    public async Task GetMyAssignmentsAsync_IncludesCorrectRole()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Active);
        await CreateParticipantAsync(exercise.Id, _testUserId, ExerciseRole.Evaluator);

        // Act
        var result = await _service.GetMyAssignmentsAsync(_testUserId);

        // Assert
        Assert.Single(result.Active);
        Assert.Equal("Evaluator", result.Active[0].Role);
    }

    // =========================================================================
    // GetAssignmentAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetAssignmentAsync_ExistingAssignment_ReturnsAssignment()
    {
        // Arrange
        var exercise = await CreateExerciseAsync(ExerciseStatus.Active);
        await CreateParticipantAsync(exercise.Id, _testUserId, ExerciseRole.Controller);

        // Act
        var result = await _service.GetAssignmentAsync(_testUserId, exercise.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(exercise.Id, result.ExerciseId);
        Assert.Equal("Controller", result.Role);
    }

    [Fact]
    public async Task GetAssignmentAsync_NonExistentAssignment_ReturnsNull()
    {
        // Arrange - no assignment exists

        // Act
        var result = await _service.GetAssignmentAsync(_testUserId, Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }
}
