using Cadence.Core.Data;
using Cadence.Core.Features.Phases.Models.DTOs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

// Note: Normally we'd use WebApi project reference, but for now we test the logic via repository pattern
// These tests validate the business logic that would be in PhasesController

namespace Cadence.Core.Tests.Features.Phases;

public class PhasesControllerTests
{
    private readonly Mock<ILogger<PhasesControllerTestHelper>> _loggerMock;

    public PhasesControllerTests()
    {
        _loggerMock = new Mock<ILogger<PhasesControllerTestHelper>>();
    }

    private (AppDbContext context, Organization org, Exercise exercise) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid(),
            ModifiedBy = Guid.NewGuid()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        return (context, org, exercise);
    }

    #region GetPhases Tests

    [Fact]
    public async Task GetPhases_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhases(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPhases_NoPhases_ReturnsEmptyList()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhases(exercise.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var phases = okResult.Value.Should().BeAssignableTo<IEnumerable<PhaseDto>>().Subject;
        phases.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPhases_WithPhases_ReturnsPhasesOrderedBySequence()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();

        var phase1 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 2",
            Sequence = 2,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        var phase2 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 1",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.AddRange(phase1, phase2);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhases(exercise.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var phases = okResult.Value.Should().BeAssignableTo<IEnumerable<PhaseDto>>().Subject.ToList();
        phases.Should().HaveCount(2);
        phases[0].Name.Should().Be("Phase 1");
        phases[1].Name.Should().Be("Phase 2");
    }

    [Fact]
    public async Task GetPhases_WithInjects_ReturnsCorrectInjectCounts()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase with injects",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Msels.Add(msel);

        // Add 3 injects to the phase
        for (int i = 0; i < 3; i++)
        {
            context.Injects.Add(new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = i + 1,
                Title = $"Inject {i + 1}",
                Description = "Test",
                ScheduledTime = new TimeOnly(9, 0),
                Target = "Target",
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = i + 1,
                MselId = msel.Id,
                PhaseId = phase.Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            });
        }
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhases(exercise.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var phases = okResult.Value.Should().BeAssignableTo<IEnumerable<PhaseDto>>().Subject.ToList();
        phases.Should().HaveCount(1);
        phases[0].InjectCount.Should().Be(3);
    }

    #endregion

    #region GetPhase Tests

    [Fact]
    public async Task GetPhase_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhase(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPhase_PhaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhase(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPhase_ValidPhase_ReturnsPhaseDto()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Test Phase",
            Description = "Test Description",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.GetPhase(exercise.Id, phase.Id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<PhaseDto>().Subject;
        dto.Name.Should().Be("Test Phase");
        dto.Description.Should().Be("Test Description");
    }

    #endregion

    #region CreatePhase Tests

    [Fact]
    public async Task CreatePhase_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "Test Phase" };

        // Act
        var result = await helper.CreatePhase(Guid.NewGuid(), request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreatePhase_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "" };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Name is required" });
    }

    [Fact]
    public async Task CreatePhase_NameTooShort_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "AB" };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Name must be at least 3 characters" });
    }

    [Fact]
    public async Task CreatePhase_NameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = new string('A', 101) };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Name must be 100 characters or less" });
    }

    [Fact]
    public async Task CreatePhase_DescriptionTooLong_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "Valid Name", Description = new string('A', 501) };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Description must be 500 characters or less" });
    }

    [Fact]
    public async Task CreatePhase_ArchivedExercise_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        exercise.Status = ExerciseStatus.Archived;
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "Test Phase" };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Archived exercises cannot be modified" });
    }

    [Fact]
    public async Task CreatePhase_ValidRequest_CreatesPhaseWithNextSequence()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();

        // Add existing phase
        context.Phases.Add(new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Existing Phase",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        });
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "New Phase", Description = "Description" };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<PhaseDto>().Subject;
        dto.Name.Should().Be("New Phase");
        dto.Description.Should().Be("Description");
        dto.Sequence.Should().Be(2); // Next sequence after existing
        dto.InjectCount.Should().Be(0);
    }

    [Fact]
    public async Task CreatePhase_FirstPhase_SequenceIsOne()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new CreatePhaseRequest { Name = "First Phase" };

        // Act
        var result = await helper.CreatePhase(exercise.Id, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = createdResult.Value.Should().BeOfType<PhaseDto>().Subject;
        dto.Sequence.Should().Be(1);
    }

    #endregion

    #region UpdatePhase Tests

    [Fact]
    public async Task UpdatePhase_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new UpdatePhaseRequest { Name = "Updated" };

        // Act
        var result = await helper.UpdatePhase(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdatePhase_PhaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new UpdatePhaseRequest { Name = "Updated" };

        // Act
        var result = await helper.UpdatePhase(exercise.Id, Guid.NewGuid(), request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdatePhase_InvalidName_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new UpdatePhaseRequest { Name = "AB" };

        // Act
        var result = await helper.UpdatePhase(exercise.Id, phase.Id, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdatePhase_ArchivedExercise_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        exercise.Status = ExerciseStatus.Archived;
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new UpdatePhaseRequest { Name = "Updated Name" };

        // Act
        var result = await helper.UpdatePhase(exercise.Id, phase.Id, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdatePhase_ValidRequest_UpdatesPhase()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            Description = "Original Desc",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new UpdatePhaseRequest { Name = "Updated Name", Description = "Updated Desc" };

        // Act
        var result = await helper.UpdatePhase(exercise.Id, phase.Id, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<PhaseDto>().Subject;
        dto.Name.Should().Be("Updated Name");
        dto.Description.Should().Be("Updated Desc");

        // Verify in database
        var updated = await context.Phases.FindAsync(phase.Id);
        updated!.Name.Should().Be("Updated Name");
    }

    #endregion

    #region DeletePhase Tests

    [Fact]
    public async Task DeletePhase_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.DeletePhase(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePhase_PhaseNotFound_ReturnsNotFound()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.DeletePhase(exercise.Id, Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePhase_ArchivedExercise_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        exercise.Status = ExerciseStatus.Archived;
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.DeletePhase(exercise.Id, phase.Id);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeletePhase_PhaseHasInjects_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase with injects",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Msels.Add(msel);

        context.Injects.Add(new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 1,
            Title = "Inject 1",
            Description = "Test",
            ScheduledTime = new TimeOnly(9, 0),
            Target = "Target",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Pending,
            Sequence = 1,
            MselId = msel.Id,
            PhaseId = phase.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        });
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.DeletePhase(exercise.Id, phase.Id);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "Cannot delete phase with 1 inject(s). Move or delete the injects first." });
    }

    [Fact]
    public async Task DeletePhase_NoInjects_DeletesPhase()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase to delete",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);

        // Act
        var result = await helper.DeletePhase(exercise.Id, phase.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        // Verify deleted
        var deleted = await context.Phases.FindAsync(phase.Id);
        deleted.Should().BeNull();
    }

    #endregion

    #region ReorderPhases Tests

    [Fact]
    public async Task ReorderPhases_ExerciseNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var result = await helper.ReorderPhases(Guid.NewGuid(), request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReorderPhases_ArchivedExercise_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        exercise.Status = ExerciseStatus.Archived;
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var result = await helper.ReorderPhases(exercise.Id, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReorderPhases_EmptyList_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid>() };

        // Act
        var result = await helper.ReorderPhases(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = "PhaseIds list is required" });
    }

    [Fact]
    public async Task ReorderPhases_InvalidPhaseId_ReturnsBadRequest()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 1",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.Add(phase);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        var invalidId = Guid.NewGuid();
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase.Id, invalidId } };

        // Act
        var result = await helper.ReorderPhases(exercise.Id, request);

        // Assert
        var badResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badResult.Value.Should().BeEquivalentTo(new { message = $"Phase {invalidId} not found in this exercise" });
    }

    [Fact]
    public async Task ReorderPhases_ValidRequest_UpdatesSequences()
    {
        // Arrange
        var (context, _, exercise) = CreateTestContext();
        var phase1 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 1",
            Sequence = 1,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        var phase2 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 2",
            Sequence = 2,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        var phase3 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 3",
            Sequence = 3,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
        context.Phases.AddRange(phase1, phase2, phase3);
        await context.SaveChangesAsync();

        var helper = new PhasesControllerTestHelper(context, _loggerMock.Object);
        // Reverse order
        var request = new ReorderPhasesRequest { PhaseIds = new List<Guid> { phase3.Id, phase2.Id, phase1.Id } };

        // Act
        var result = await helper.ReorderPhases(exercise.Id, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var phases = okResult.Value.Should().BeAssignableTo<IEnumerable<PhaseDto>>().Subject.ToList();
        phases.Should().HaveCount(3);
        phases[0].Name.Should().Be("Phase 3");
        phases[0].Sequence.Should().Be(1);
        phases[1].Name.Should().Be("Phase 2");
        phases[1].Sequence.Should().Be(2);
        phases[2].Name.Should().Be("Phase 1");
        phases[2].Sequence.Should().Be(3);
    }

    #endregion
}

/// <summary>
/// Helper class that mirrors PhasesController logic for testing without WebApi dependency.
/// This allows us to test the business logic in Core.Tests project.
/// </summary>
public class PhasesControllerTestHelper
{
    private readonly AppDbContext _context;
    private readonly ILogger<PhasesControllerTestHelper> _logger;

    public PhasesControllerTestHelper(AppDbContext context, ILogger<PhasesControllerTestHelper> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> GetPhases(Guid exerciseId)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .OrderBy(p => p.Sequence)
            .ToListAsync();

        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count);

        var dtos = phases.Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0)));
        return new OkObjectResult(dtos);
    }

    public async Task<IActionResult> GetPhase(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return new NotFoundObjectResult(new { message = "Phase not found" });
        }

        var injectCount = await _context.Injects.CountAsync(i => i.PhaseId == id);
        return new OkObjectResult(phase.ToDto(injectCount));
    }

    public async Task<IActionResult> CreatePhase(Guid exerciseId, CreatePhaseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        var validationError = ValidatePhaseRequest(request.Name, request.Description);
        if (validationError != null)
        {
            return new BadRequestObjectResult(new { message = validationError });
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            return new BadRequestObjectResult(new { message = "Archived exercises cannot be modified" });
        }

        var maxSequence = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .MaxAsync(p => (int?)p.Sequence) ?? 0;

        var phase = request.ToEntity(exerciseId, maxSequence + 1, Guid.Empty);
        _context.Phases.Add(phase);
        await _context.SaveChangesAsync();

        return new CreatedAtActionResult("GetPhase", "Phases", new { exerciseId, id = phase.Id }, phase.ToDto(0));
    }

    public async Task<IActionResult> UpdatePhase(Guid exerciseId, Guid id, UpdatePhaseRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return new NotFoundObjectResult(new { message = "Phase not found" });
        }

        var validationError = ValidatePhaseRequest(request.Name, request.Description);
        if (validationError != null)
        {
            return new BadRequestObjectResult(new { message = validationError });
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            return new BadRequestObjectResult(new { message = "Archived exercises cannot be modified" });
        }

        phase.UpdateFromRequest(request, Guid.Empty);
        await _context.SaveChangesAsync();

        var injectCount = await _context.Injects.CountAsync(i => i.PhaseId == id);
        return new OkObjectResult(phase.ToDto(injectCount));
    }

    public async Task<IActionResult> DeletePhase(Guid exerciseId, Guid id)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            return new BadRequestObjectResult(new { message = "Archived exercises cannot be modified" });
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(p => p.Id == id && p.ExerciseId == exerciseId);

        if (phase == null)
        {
            return new NotFoundObjectResult(new { message = "Phase not found" });
        }

        var injectCount = await _context.Injects.CountAsync(i => i.PhaseId == id);
        if (injectCount > 0)
        {
            return new BadRequestObjectResult(new { message = $"Cannot delete phase with {injectCount} inject(s). Move or delete the injects first." });
        }

        _context.Phases.Remove(phase);
        await _context.SaveChangesAsync();

        return new NoContentResult();
    }

    public async Task<IActionResult> ReorderPhases(Guid exerciseId, ReorderPhasesRequest request)
    {
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            return new NotFoundObjectResult(new { message = "Exercise not found" });
        }

        if (exercise.Status == ExerciseStatus.Archived)
        {
            return new BadRequestObjectResult(new { message = "Archived exercises cannot be modified" });
        }

        if (request.PhaseIds.Count == 0)
        {
            return new BadRequestObjectResult(new { message = "PhaseIds list is required" });
        }

        var phases = await _context.Phases
            .Where(p => p.ExerciseId == exerciseId)
            .ToListAsync();

        var phaseDict = phases.ToDictionary(p => p.Id);
        foreach (var phaseId in request.PhaseIds)
        {
            if (!phaseDict.ContainsKey(phaseId))
            {
                return new BadRequestObjectResult(new { message = $"Phase {phaseId} not found in this exercise" });
            }
        }

        for (int i = 0; i < request.PhaseIds.Count; i++)
        {
            var phase = phaseDict[request.PhaseIds[i]];
            phase.Sequence = i + 1;
            phase.ModifiedBy = Guid.Empty;
        }

        await _context.SaveChangesAsync();

        var injectCounts = await _context.Injects
            .Where(i => i.Msel!.ExerciseId == exerciseId && i.PhaseId != null)
            .GroupBy(i => i.PhaseId)
            .Select(g => new { PhaseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PhaseId!.Value, x => x.Count);

        var orderedPhases = phases
            .OrderBy(p => p.Sequence)
            .Select(p => p.ToDto(injectCounts.GetValueOrDefault(p.Id, 0)));

        return new OkObjectResult(orderedPhases);
    }

    private static string? ValidatePhaseRequest(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required";
        }
        if (name.Length < 3)
        {
            return "Name must be at least 3 characters";
        }
        if (name.Length > 100)
        {
            return "Name must be 100 characters or less";
        }
        if (description?.Length > 500)
        {
            return "Description must be 500 characters or less";
        }
        return null;
    }
}
