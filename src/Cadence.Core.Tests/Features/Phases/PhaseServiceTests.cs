using Cadence.Core.Data;
using Cadence.Core.Features.Phases.Models.DTOs;
using Cadence.Core.Features.Phases.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Phases;

/// <summary>
/// Unit tests for <see cref="PhaseService"/>.
/// Covers CRUD operations, sequence management, archived-exercise guard, and inject-linked deletion prevention.
/// </summary>
public class PhaseServiceTests
{
    private readonly Mock<ILogger<PhaseService>> _loggerMock = new();
    private const string TestUser = "test-user-id";

    // =========================================================================
    // Test Setup Helpers
    // =========================================================================

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext(
        ExerciseStatus status = ExerciseStatus.Draft)
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Hurricane Response TTX",
            ExerciseType = ExerciseType.TTX,
            Status = status,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, org, exercise);
    }

    private Phase CreatePhase(
        AppDbContext context,
        Guid exerciseId,
        Guid organizationId,
        int sequence,
        string name = "Test Phase")
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Sequence = sequence,
            ExerciseId = exerciseId,
            OrganizationId = organizationId,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Phases.Add(phase);
        context.SaveChanges();
        return phase;
    }

    private (Msel msel, Inject inject) CreateInjectInPhase(
        AppDbContext context,
        Guid exerciseId,
        Guid phaseId)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exerciseId,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Msels.Add(msel);

        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Test Inject",
            Description = "Test inject description",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "EOC Team",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Draft,
            Sequence = 1,
            MselId = msel.Id,
            PhaseId = phaseId,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Injects.Add(inject);
        context.SaveChanges();
        return (msel, inject);
    }

    private PhaseService CreateService(AppDbContext context) =>
        new(context, _loggerMock.Object);

    // =========================================================================
    // GetPhasesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetPhasesAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhasesAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhasesAsync_NoPhases_ReturnsEmptyList()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhasesAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPhasesAsync_ReturnsPhasesOrderedBySequence()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreatePhase(context, exercise.Id, org.Id, 3, "Recovery");
        CreatePhase(context, exercise.Id, org.Id, 1, "Initial Response");
        CreatePhase(context, exercise.Id, org.Id, 2, "Stabilization");
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetPhasesAsync(exercise.Id))!;

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Initial Response");
        result[1].Name.Should().Be("Stabilization");
        result[2].Name.Should().Be("Recovery");
    }

    [Fact]
    public async Task GetPhasesAsync_WithInjects_ReturnsCorrectInjectCounts()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Phase With Injects");
        CreateInjectInPhase(context, exercise.Id, phase.Id);
        CreateInjectInPhase(context, exercise.Id, phase.Id);
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetPhasesAsync(exercise.Id))!;

        // Assert
        result.Single().InjectCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPhasesAsync_PhaseWithNoInjects_ReturnsZeroInjectCount()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreatePhase(context, exercise.Id, org.Id, 1, "Empty Phase");
        var sut = CreateService(context);

        // Act
        var result = (await sut.GetPhasesAsync(exercise.Id))!;

        // Assert
        result.Single().InjectCount.Should().Be(0);
    }

    // =========================================================================
    // GetPhaseAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetPhaseAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhaseAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhaseAsync_PhaseNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhaseAsync(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPhaseAsync_ValidPhase_ReturnsPhaseDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Initial Response");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhaseAsync(exercise.Id, phase.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(phase.Id);
        result.Name.Should().Be("Initial Response");
        result.Sequence.Should().Be(1);
        result.ExerciseId.Should().Be(exercise.Id);
    }

    [Fact]
    public async Task GetPhaseAsync_PhaseBelongsToDifferentExercise_ReturnsNull()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var otherExercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Other Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = TestUser,
            ModifiedBy = TestUser
        };
        context.Exercises.Add(otherExercise);
        context.SaveChanges();

        var phase = CreatePhase(context, otherExercise.Id, org.Id, 1, "Other Phase");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetPhaseAsync(exercise.Id, phase.Id);

        // Assert
        result.Should().BeNull();
    }

    // =========================================================================
    // CreatePhaseAsync Tests
    // =========================================================================

    [Fact]
    public async Task CreatePhaseAsync_ValidRequest_ReturnsCreatedPhase()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var request = new CreatePhaseRequest { Name = "Initial Response", Description = "First hour of exercise" };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreatePhaseAsync(exercise.Id, request, TestUser);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Initial Response");
        result.Description.Should().Be("First hour of exercise");
        result.ExerciseId.Should().Be(exercise.Id);
        result.InjectCount.Should().Be(0);
    }

    [Fact]
    public async Task CreatePhaseAsync_FirstPhase_SequenceIsOne()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "First Phase" }, TestUser);

        // Assert
        result.Sequence.Should().Be(1);
    }

    [Fact]
    public async Task CreatePhaseAsync_AddsAfterExistingPhases_SequenceIsNext()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        CreatePhase(context, exercise.Id, org.Id, 1, "Phase One");
        CreatePhase(context, exercise.Id, org.Id, 2, "Phase Two");
        var sut = CreateService(context);

        // Act
        var result = await sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "Phase Three" }, TestUser);

        // Assert
        result.Sequence.Should().Be(3);
    }

    [Fact]
    public async Task CreatePhaseAsync_ExerciseNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(Guid.NewGuid(), new CreatePhaseRequest { Name = "New Phase" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreatePhaseAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "New Phase" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task CreatePhaseAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public async Task CreatePhaseAsync_NameTooShort_ThrowsArgumentException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "AB" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*at least 3 characters*");
    }

    [Fact]
    public async Task CreatePhaseAsync_NameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = new string('X', 101) }, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*100 characters or less*");
    }

    [Fact]
    public async Task CreatePhaseAsync_DescriptionTooLong_ThrowsArgumentException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var act = () => sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest
        {
            Name = "Valid Name",
            Description = new string('D', 501)
        }, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*500 characters or less*");
    }

    [Fact]
    public async Task CreatePhaseAsync_SetsOrganizationIdFromExercise()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        await sut.CreatePhaseAsync(exercise.Id, new CreatePhaseRequest { Name = "Phase One" }, TestUser);

        // Assert
        var persisted = context.Phases.Single();
        persisted.OrganizationId.Should().Be(org.Id);
    }

    [Fact]
    public async Task CreatePhaseAsync_WithStartAndEndTime_PersistsTimeValues()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var start = new TimeOnly(8, 0);
        var end = new TimeOnly(12, 0);
        var request = new CreatePhaseRequest { Name = "Morning Phase", StartTime = start, EndTime = end };
        var sut = CreateService(context);

        // Act
        var result = await sut.CreatePhaseAsync(exercise.Id, request, TestUser);

        // Assert
        result.StartTime.Should().Be(start);
        result.EndTime.Should().Be(end);
    }

    // =========================================================================
    // UpdatePhaseAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdatePhaseAsync_ValidRequest_ReturnsUpdatedDto()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Original Name");
        var request = new UpdatePhaseRequest { Name = "Updated Name", Description = "New description" };
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdatePhaseAsync(exercise.Id, phase.Id, request, TestUser);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("New description");
    }

    [Fact]
    public async Task UpdatePhaseAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdatePhaseAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdatePhaseRequest { Name = "Update" }, TestUser);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePhaseAsync_PhaseNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdatePhaseAsync(exercise.Id, Guid.NewGuid(), new UpdatePhaseRequest { Name = "Update" }, TestUser);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePhaseAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Archived Phase");
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdatePhaseAsync(exercise.Id, phase.Id, new UpdatePhaseRequest { Name = "Try Update" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task UpdatePhaseAsync_InvalidName_ThrowsArgumentException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Valid Phase");
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdatePhaseAsync(exercise.Id, phase.Id, new UpdatePhaseRequest { Name = "AB" }, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdatePhaseAsync_PersistsChangesToDatabase()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Original");
        var sut = CreateService(context);

        // Act
        await sut.UpdatePhaseAsync(exercise.Id, phase.Id, new UpdatePhaseRequest { Name = "Persisted Update" }, TestUser);

        // Assert
        var persisted = await context.Phases.FindAsync(phase.Id);
        persisted!.Name.Should().Be("Persisted Update");
    }

    [Fact]
    public async Task UpdatePhaseAsync_ReturnsCurrentInjectCount()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Phase");
        CreateInjectInPhase(context, exercise.Id, phase.Id);
        CreateInjectInPhase(context, exercise.Id, phase.Id);
        var sut = CreateService(context);

        // Act
        var result = await sut.UpdatePhaseAsync(exercise.Id, phase.Id, new UpdatePhaseRequest { Name = "Updated Phase" }, TestUser);

        // Assert
        result!.InjectCount.Should().Be(2);
    }

    // =========================================================================
    // DeletePhaseAsync Tests
    // =========================================================================

    [Fact]
    public async Task DeletePhaseAsync_PhaseExists_ReturnsTrue()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Delete Me");
        var sut = CreateService(context);

        // Act
        var result = await sut.DeletePhaseAsync(exercise.Id, phase.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePhaseAsync_ExerciseNotFound_ReturnsFalse()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.DeletePhaseAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePhaseAsync_PhaseNotFound_ReturnsFalse()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.DeletePhaseAsync(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeletePhaseAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Phase");
        var sut = CreateService(context);

        // Act
        var act = () => sut.DeletePhaseAsync(exercise.Id, phase.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task DeletePhaseAsync_PhaseHasInjects_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Phase With Injects");
        CreateInjectInPhase(context, exercise.Id, phase.Id);
        var sut = CreateService(context);

        // Act
        var act = () => sut.DeletePhaseAsync(exercise.Id, phase.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot delete phase with 1 inject(s)*");
    }

    [Fact]
    public async Task DeletePhaseAsync_HardDeletesRecord()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Hard Delete Test");
        var sut = CreateService(context);

        // Act
        await sut.DeletePhaseAsync(exercise.Id, phase.Id);

        // Assert — phases use hard delete, so the record should be completely gone
        var persisted = await context.Phases.FindAsync(phase.Id);
        persisted.Should().BeNull();
    }

    // =========================================================================
    // ReorderPhasesAsync Tests
    // =========================================================================

    [Fact]
    public async Task ReorderPhasesAsync_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var result = await sut.ReorderPhasesAsync(Guid.NewGuid(), request, TestUser);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ReorderPhasesAsync_ArchivedExercise_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext(ExerciseStatus.Archived);
        var sut = CreateService(context);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var act = () => sut.ReorderPhasesAsync(exercise.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Archived exercises cannot be modified*");
    }

    [Fact]
    public async Task ReorderPhasesAsync_EmptyPhaseIds_ThrowsArgumentException()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var sut = CreateService(context);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid>() };

        // Act
        var act = () => sut.ReorderPhasesAsync(exercise.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public async Task ReorderPhasesAsync_PhaseIdNotInExercise_ThrowsArgumentException()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase = CreatePhase(context, exercise.Id, org.Id, 1, "Phase One");
        var bogusId = Guid.NewGuid();
        var sut = CreateService(context);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase.Id, bogusId } };

        // Act
        var act = () => sut.ReorderPhasesAsync(exercise.Id, request, TestUser);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{bogusId}*not found*");
    }

    [Fact]
    public async Task ReorderPhasesAsync_ValidRequest_UpdatesSequencesCorrectly()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase1 = CreatePhase(context, exercise.Id, org.Id, 1, "Phase A");
        var phase2 = CreatePhase(context, exercise.Id, org.Id, 2, "Phase B");
        var phase3 = CreatePhase(context, exercise.Id, org.Id, 3, "Phase C");
        var sut = CreateService(context);

        // Reverse the order
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase3.Id, phase2.Id, phase1.Id } };

        // Act
        var result = (await sut.ReorderPhasesAsync(exercise.Id, request, TestUser))!;

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Phase C");
        result[0].Sequence.Should().Be(1);
        result[1].Name.Should().Be("Phase B");
        result[1].Sequence.Should().Be(2);
        result[2].Name.Should().Be("Phase A");
        result[2].Sequence.Should().Be(3);
    }

    [Fact]
    public async Task ReorderPhasesAsync_PersistsNewSequencesToDatabase()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase1 = CreatePhase(context, exercise.Id, org.Id, 1, "Phase A");
        var phase2 = CreatePhase(context, exercise.Id, org.Id, 2, "Phase B");
        var sut = CreateService(context);

        // Swap order
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase2.Id, phase1.Id } };

        // Act
        await sut.ReorderPhasesAsync(exercise.Id, request, TestUser);

        // Assert
        var persistedA = await context.Phases.FindAsync(phase1.Id);
        var persistedB = await context.Phases.FindAsync(phase2.Id);
        persistedA!.Sequence.Should().Be(2);
        persistedB!.Sequence.Should().Be(1);
    }

    [Fact]
    public async Task ReorderPhasesAsync_ReturnsListOrderedByNewSequence()
    {
        // Arrange
        var (context, org, exercise) = CreateTestContext();
        var phase1 = CreatePhase(context, exercise.Id, org.Id, 1, "First");
        var phase2 = CreatePhase(context, exercise.Id, org.Id, 2, "Second");
        var sut = CreateService(context);

        // Swap
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase2.Id, phase1.Id } };

        // Act
        var result = (await sut.ReorderPhasesAsync(exercise.Id, request, TestUser))!;

        // Assert — result should be sorted by sequence ascending
        result[0].Name.Should().Be("Second");
        result[1].Name.Should().Be("First");
    }
}
