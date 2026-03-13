using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Exercises;

/// <summary>
/// Tests for <see cref="SetupProgressService"/> - exercise setup completion scoring.
/// </summary>
public class SetupProgressServiceTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    private Exercise CreateExercise(
        AppDbContext context,
        Organization org,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            StartTime = startTime,
            EndTime = endTime,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();
        return exercise;
    }

    private Msel CreateActiveMsel(AppDbContext context, Organization org, Exercise exercise)
    {
        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "v1.0",
            Version = 1,
            IsActive = true,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(msel);
        context.SaveChanges();
        return msel;
    }

    private Inject CreateInject(AppDbContext context, Msel msel, int injectNumber = 1)
    {
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = injectNumber,
            Title = $"Inject {injectNumber}",
            Status = InjectStatus.Draft,
            Sequence = injectNumber,
            MselId = msel.Id,
            TriggerType = TriggerType.Manual,
            InjectType = InjectType.Standard,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Injects.Add(inject);
        context.SaveChanges();
        return inject;
    }

    private ExerciseParticipant AddParticipant(
        AppDbContext context,
        Exercise exercise,
        ExerciseRole role)
    {
        var participant = new ExerciseParticipant
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            UserId = Guid.NewGuid().ToString(),
            Role = role,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.ExerciseParticipants.Add(participant);
        context.SaveChanges();
        return participant;
    }

    private Phase AddPhase(AppDbContext context, Organization org, Exercise exercise)
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 1",
            Sequence = 1,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Phases.Add(phase);
        context.SaveChanges();
        return phase;
    }

    private Objective AddObjective(AppDbContext context, Organization org, Exercise exercise)
    {
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "1",
            Name = "Objective 1",
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Objectives.Add(objective);
        context.SaveChanges();
        return objective;
    }

    private SetupProgressService CreateService(AppDbContext context) =>
        new SetupProgressService(context);

    // =========================================================================
    // GetSetupProgressAsync — Not Found
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_ExerciseNotFound_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // =========================================================================
    // GetSetupProgressAsync — Empty Exercise (all areas incomplete)
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_EmptyExercise_AllAreasIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result.Should().NotBeNull();
        result!.OverallPercentage.Should().Be(0);
        result.IsReadyToActivate.Should().BeFalse();
        result.Areas.Should().HaveCount(5);
        result.Areas.Should().OnlyContain(a => !a.IsComplete);
    }

    [Fact]
    public async Task GetSetupProgress_EmptyExercise_ReturnsCorrectAreaIds()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.Areas.Select(a => a.Id)
            .Should().BeEquivalentTo(new[] { "msel", "participants", "phases", "objectives", "scheduling" });
    }

    [Fact]
    public async Task GetSetupProgress_EmptyExercise_ReturnsCorrectWeights()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        var participantsArea = result.Areas.Single(a => a.Id == "participants");
        var phasesArea = result.Areas.Single(a => a.Id == "phases");
        var objectivesArea = result.Areas.Single(a => a.Id == "objectives");
        var schedulingArea = result.Areas.Single(a => a.Id == "scheduling");

        mselArea.Weight.Should().Be(35);
        participantsArea.Weight.Should().Be(15);
        phasesArea.Weight.Should().Be(15);
        objectivesArea.Weight.Should().Be(15);
        schedulingArea.Weight.Should().Be(20);

        result.Areas.Sum(a => a.Weight).Should().Be(100);
    }

    // =========================================================================
    // GetSetupProgressAsync — MSEL Area
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoMsel_MselAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.IsComplete.Should().BeFalse();
        mselArea.CurrentCount.Should().Be(0);
        mselArea.StatusMessage.Should().Be("No active MSEL");
    }

    [Fact]
    public async Task GetSetupProgress_MselWithNoInjects_MselAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        CreateActiveMsel(context, org, exercise);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.IsComplete.Should().BeFalse();
        mselArea.CurrentCount.Should().Be(0);
        mselArea.StatusMessage.Should().Be("MSEL has no injects");
    }

    [Fact]
    public async Task GetSetupProgress_MselWithOneInject_MselAreaComplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.IsComplete.Should().BeTrue();
        mselArea.CurrentCount.Should().Be(1);
        mselArea.StatusMessage.Should().Be("1 inject in MSEL");
    }

    [Fact]
    public async Task GetSetupProgress_MselWithMultipleInjects_ShowsCorrectCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);
        CreateInject(context, msel, 2);
        CreateInject(context, msel, 3);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.IsComplete.Should().BeTrue();
        mselArea.CurrentCount.Should().Be(3);
        mselArea.StatusMessage.Should().Be("3 injects in MSEL");
    }

    [Fact]
    public async Task GetSetupProgress_MselWithDeletedInjects_ExcludesDeletedFromCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);

        // Add a soft-deleted inject
        var deletedInject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = 2,
            Title = "Deleted Inject",
            Status = InjectStatus.Draft,
            Sequence = 2,
            MselId = msel.Id,
            TriggerType = TriggerType.Manual,
            InjectType = InjectType.Standard,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Injects.Add(deletedInject);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.CurrentCount.Should().Be(1, "deleted injects should not count toward MSEL inject count");
    }

    [Fact]
    public async Task GetSetupProgress_InactiveMsel_NotCountedAsActiveMsel()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        // Add an inactive MSEL with injects
        var inactiveMsel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "v0.1 (Inactive)",
            Version = 1,
            IsActive = false,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Msels.Add(inactiveMsel);
        context.SaveChanges();
        CreateInject(context, inactiveMsel, 1);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var mselArea = result!.Areas.Single(a => a.Id == "msel");
        mselArea.IsComplete.Should().BeFalse("only active MSELs count toward setup progress");
        mselArea.StatusMessage.Should().Be("No active MSEL");
    }

    // =========================================================================
    // GetSetupProgressAsync — Participants Area
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoParticipants_ParticipantsAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var participantsArea = result!.Areas.Single(a => a.Id == "participants");
        participantsArea.IsComplete.Should().BeFalse();
        participantsArea.CurrentCount.Should().Be(0);
        participantsArea.StatusMessage.Should().Be("No participants assigned");
    }

    [Fact]
    public async Task GetSetupProgress_ParticipantsWithNoDirector_ParticipantsAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddParticipant(context, exercise, ExerciseRole.Controller);
        AddParticipant(context, exercise, ExerciseRole.Evaluator);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var participantsArea = result!.Areas.Single(a => a.Id == "participants");
        participantsArea.IsComplete.Should().BeFalse("an Exercise Director is required");
        participantsArea.CurrentCount.Should().Be(2);
        participantsArea.StatusMessage.Should().Be("2 participants (no Director)");
    }

    [Fact]
    public async Task GetSetupProgress_OnlyExerciseDirector_ParticipantsAreaComplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var participantsArea = result!.Areas.Single(a => a.Id == "participants");
        participantsArea.IsComplete.Should().BeTrue();
        participantsArea.StatusMessage.Should().Be("Exercise Director assigned");
    }

    [Fact]
    public async Task GetSetupProgress_DirectorPlusOthers_ShowsCorrectStatusMessage()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        AddParticipant(context, exercise, ExerciseRole.Controller);
        AddParticipant(context, exercise, ExerciseRole.Evaluator);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var participantsArea = result!.Areas.Single(a => a.Id == "participants");
        participantsArea.IsComplete.Should().BeTrue();
        participantsArea.CurrentCount.Should().Be(3);
        participantsArea.StatusMessage.Should().Be("Exercise Director + 2 others");
    }

    [Fact]
    public async Task GetSetupProgress_DirectorPlusOneOther_UsesCorrectSingularForm()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        AddParticipant(context, exercise, ExerciseRole.Controller);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var participantsArea = result!.Areas.Single(a => a.Id == "participants");
        participantsArea.StatusMessage.Should().Be("Exercise Director + 1 other");
    }

    // =========================================================================
    // GetSetupProgressAsync — Phases Area
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoPhases_PhasesAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var phasesArea = result!.Areas.Single(a => a.Id == "phases");
        phasesArea.IsComplete.Should().BeFalse();
        phasesArea.CurrentCount.Should().Be(0);
        phasesArea.StatusMessage.Should().Be("No phases defined");
    }

    [Fact]
    public async Task GetSetupProgress_OnePhase_PhasesAreaComplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddPhase(context, org, exercise);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var phasesArea = result!.Areas.Single(a => a.Id == "phases");
        phasesArea.IsComplete.Should().BeTrue();
        phasesArea.CurrentCount.Should().Be(1);
        phasesArea.StatusMessage.Should().Be("1 phase defined");
    }

    [Fact]
    public async Task GetSetupProgress_MultiplePhases_ShowsCorrectCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddPhase(context, org, exercise);

        var phase2 = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Phase 2",
            Sequence = 2,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Phases.Add(phase2);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var phasesArea = result!.Areas.Single(a => a.Id == "phases");
        phasesArea.IsComplete.Should().BeTrue();
        phasesArea.CurrentCount.Should().Be(2);
        phasesArea.StatusMessage.Should().Be("2 phases defined");
    }

    [Fact]
    public async Task GetSetupProgress_DeletedPhase_ExcludedFromCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);

        var deletedPhase = new Phase
        {
            Id = Guid.NewGuid(),
            Name = "Deleted Phase",
            Sequence = 1,
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Phases.Add(deletedPhase);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var phasesArea = result!.Areas.Single(a => a.Id == "phases");
        phasesArea.IsComplete.Should().BeFalse("deleted phases should not count");
        phasesArea.CurrentCount.Should().Be(0);
    }

    // =========================================================================
    // GetSetupProgressAsync — Objectives Area
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoObjectives_ObjectivesAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var objectivesArea = result!.Areas.Single(a => a.Id == "objectives");
        objectivesArea.IsComplete.Should().BeFalse();
        objectivesArea.CurrentCount.Should().Be(0);
        objectivesArea.StatusMessage.Should().Be("No objectives defined");
    }

    [Fact]
    public async Task GetSetupProgress_OneObjective_ObjectivesAreaComplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddObjective(context, org, exercise);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var objectivesArea = result!.Areas.Single(a => a.Id == "objectives");
        objectivesArea.IsComplete.Should().BeTrue();
        objectivesArea.CurrentCount.Should().Be(1);
        objectivesArea.StatusMessage.Should().Be("1 objective defined");
    }

    [Fact]
    public async Task GetSetupProgress_MultipleObjectives_ShowsCorrectCount()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        AddObjective(context, org, exercise);

        var obj2 = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "2",
            Name = "Objective 2",
            ExerciseId = exercise.Id,
            OrganizationId = org.Id,
            CreatedBy = "test-user",
            ModifiedBy = "test-user"
        };
        context.Objectives.Add(obj2);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var objectivesArea = result!.Areas.Single(a => a.Id == "objectives");
        objectivesArea.IsComplete.Should().BeTrue();
        objectivesArea.CurrentCount.Should().Be(2);
        objectivesArea.StatusMessage.Should().Be("2 objectives defined");
    }

    // =========================================================================
    // GetSetupProgressAsync — Scheduling Area
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoTimes_SchedulingAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var schedulingArea = result!.Areas.Single(a => a.Id == "scheduling");
        schedulingArea.IsComplete.Should().BeFalse();
        schedulingArea.CurrentCount.Should().Be(0);
        schedulingArea.RequiredCount.Should().Be(2);
        schedulingArea.StatusMessage.Should().Be("No times configured");
    }

    [Fact]
    public async Task GetSetupProgress_OnlyStartTime_SchedulingAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, startTime: new TimeOnly(9, 0));
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var schedulingArea = result!.Areas.Single(a => a.Id == "scheduling");
        schedulingArea.IsComplete.Should().BeFalse();
        schedulingArea.CurrentCount.Should().Be(1);
        schedulingArea.StatusMessage.Should().Be("End time not set");
    }

    [Fact]
    public async Task GetSetupProgress_OnlyEndTime_SchedulingAreaIncomplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org, endTime: new TimeOnly(17, 0));
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var schedulingArea = result!.Areas.Single(a => a.Id == "scheduling");
        schedulingArea.IsComplete.Should().BeFalse();
        schedulingArea.CurrentCount.Should().Be(1);
        schedulingArea.StatusMessage.Should().Be("Start time not set");
    }

    [Fact]
    public async Task GetSetupProgress_BothTimes_SchedulingAreaComplete()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(17, 0));
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        var schedulingArea = result!.Areas.Single(a => a.Id == "scheduling");
        schedulingArea.IsComplete.Should().BeTrue();
        schedulingArea.CurrentCount.Should().Be(2);
        schedulingArea.StatusMessage.Should().Be("Start and end times configured");
    }

    // =========================================================================
    // GetSetupProgressAsync — Overall Percentage
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_AllAreasComplete_Returns100Percent()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(17, 0));

        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        AddPhase(context, org, exercise);
        AddObjective(context, org, exercise);

        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.OverallPercentage.Should().Be(100);
        result.IsReadyToActivate.Should().BeTrue();
        result.Areas.Should().OnlyContain(a => a.IsComplete);
    }

    [Fact]
    public async Task GetSetupProgress_OnlyMselComplete_Returns35Percent()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.OverallPercentage.Should().Be(35, "MSEL area has weight 35");
        result.IsReadyToActivate.Should().BeTrue("MSEL with inject is all that is required to activate");
    }

    [Fact]
    public async Task GetSetupProgress_SchedulingAndParticipantsComplete_Returns35Percent()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(17, 0));
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.OverallPercentage.Should().Be(35, "scheduling (20) + participants (15) = 35");
        result.IsReadyToActivate.Should().BeFalse("no active MSEL with injects");
    }

    // =========================================================================
    // GetSetupProgressAsync — IsReadyToActivate
    // =========================================================================

    [Fact]
    public async Task GetSetupProgress_NoMsel_NotReadyToActivate()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(17, 0));
        AddParticipant(context, exercise, ExerciseRole.ExerciseDirector);
        AddPhase(context, org, exercise);
        AddObjective(context, org, exercise);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.IsReadyToActivate.Should().BeFalse("MSEL with at least one inject is required to activate");
    }

    [Fact]
    public async Task GetSetupProgress_MselWithInject_ReadyToActivate()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var exercise = CreateExercise(context, org);
        var msel = CreateActiveMsel(context, org, exercise);
        CreateInject(context, msel, 1);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetSetupProgressAsync(exercise.Id);

        // Assert
        result!.IsReadyToActivate.Should().BeTrue();
    }
}
