using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for calculating exercise setup progress.
///
/// Setup areas and weights:
/// - MSEL (35%): Active MSEL with at least 1 inject
/// - Participants (15%): At least 1 participant (ideally an Exercise Director)
/// - Phases (15%): At least 1 phase defined
/// - Objectives (15%): At least 1 objective defined
/// - Scheduling (20%): Start time and end time set
/// </summary>
public class SetupProgressService : ISetupProgressService
{
    private readonly AppDbContext _context;

    // Weights for each area (must sum to 100)
    private const int MSEL_WEIGHT = 35;
    private const int PARTICIPANTS_WEIGHT = 15;
    private const int PHASES_WEIGHT = 15;
    private const int OBJECTIVES_WEIGHT = 15;
    private const int SCHEDULING_WEIGHT = 20;

    public SetupProgressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SetupProgressDto?> GetSetupProgressAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .Include(e => e.Phases.Where(p => !p.IsDeleted))
            .Include(e => e.Objectives.Where(o => !o.IsDeleted))
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get active MSEL with inject count
        var mselInfo = await _context.Msels
            .Where(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted)
            .Select(m => new
            {
                HasMsel = true,
                InjectCount = m.Injects.Count(i => !i.IsDeleted)
            })
            .FirstOrDefaultAsync();

        var areas = new List<SetupAreaDto>();

        // 1. MSEL Area (35%)
        var hasMsel = mselInfo?.HasMsel ?? false;
        var injectCount = mselInfo?.InjectCount ?? 0;
        var mselComplete = hasMsel && injectCount > 0;
        areas.Add(new SetupAreaDto
        {
            Id = "msel",
            Name = "MSEL & Injects",
            Description = "Active MSEL with at least one inject",
            IsComplete = mselComplete,
            Weight = MSEL_WEIGHT,
            CurrentCount = injectCount,
            RequiredCount = 1,
            StatusMessage = GetMselStatusMessage(hasMsel, injectCount)
        });

        // 2. Participants Area (15%)
        var participantCount = exercise.Participants.Count;
        var hasDirector = exercise.Participants.Any(p => p.Role == ExerciseRole.ExerciseDirector);
        var participantsComplete = hasDirector;
        areas.Add(new SetupAreaDto
        {
            Id = "participants",
            Name = "Participants",
            Description = "At least an Exercise Director assigned",
            IsComplete = participantsComplete,
            Weight = PARTICIPANTS_WEIGHT,
            CurrentCount = participantCount,
            RequiredCount = 1,
            StatusMessage = GetParticipantsStatusMessage(participantCount, hasDirector)
        });

        // 3. Phases Area (15%)
        var phaseCount = exercise.Phases.Count;
        var phasesComplete = phaseCount > 0;
        areas.Add(new SetupAreaDto
        {
            Id = "phases",
            Name = "Phases",
            Description = "At least one exercise phase defined",
            IsComplete = phasesComplete,
            Weight = PHASES_WEIGHT,
            CurrentCount = phaseCount,
            RequiredCount = 1,
            StatusMessage = phasesComplete
                ? $"{phaseCount} phase{(phaseCount != 1 ? "s" : "")} defined"
                : "No phases defined"
        });

        // 4. Objectives Area (15%)
        var objectiveCount = exercise.Objectives.Count;
        var objectivesComplete = objectiveCount > 0;
        areas.Add(new SetupAreaDto
        {
            Id = "objectives",
            Name = "Objectives",
            Description = "At least one exercise objective defined",
            IsComplete = objectivesComplete,
            Weight = OBJECTIVES_WEIGHT,
            CurrentCount = objectiveCount,
            RequiredCount = 1,
            StatusMessage = objectivesComplete
                ? $"{objectiveCount} objective{(objectiveCount != 1 ? "s" : "")} defined"
                : "No objectives defined"
        });

        // 5. Scheduling Area (20%)
        var hasStartTime = exercise.StartTime.HasValue;
        var hasEndTime = exercise.EndTime.HasValue;
        var schedulingComplete = hasStartTime && hasEndTime;
        areas.Add(new SetupAreaDto
        {
            Id = "scheduling",
            Name = "Scheduling",
            Description = "Start time and end time configured",
            IsComplete = schedulingComplete,
            Weight = SCHEDULING_WEIGHT,
            CurrentCount = (hasStartTime ? 1 : 0) + (hasEndTime ? 1 : 0),
            RequiredCount = 2,
            StatusMessage = GetSchedulingStatusMessage(hasStartTime, hasEndTime)
        });

        // Calculate overall percentage
        var completedWeight = areas.Where(a => a.IsComplete).Sum(a => a.Weight);
        var overallPercentage = completedWeight;

        // Must have at least MSEL with injects to activate
        var isReadyToActivate = mselComplete;

        return new SetupProgressDto
        {
            OverallPercentage = overallPercentage,
            IsReadyToActivate = isReadyToActivate,
            Areas = areas
        };
    }

    private static string GetMselStatusMessage(bool hasMsel, int injectCount)
    {
        if (!hasMsel)
            return "No active MSEL";

        if (injectCount == 0)
            return "MSEL has no injects";

        return $"{injectCount} inject{(injectCount != 1 ? "s" : "")} in MSEL";
    }

    private static string GetSchedulingStatusMessage(bool hasStartTime, bool hasEndTime)
    {
        if (hasStartTime && hasEndTime)
            return "Start and end times configured";

        if (hasStartTime)
            return "End time not set";

        if (hasEndTime)
            return "Start time not set";

        return "No times configured";
    }

    private static string GetParticipantsStatusMessage(int participantCount, bool hasDirector)
    {
        if (hasDirector)
        {
            return participantCount == 1
                ? "Exercise Director assigned"
                : $"Exercise Director + {participantCount - 1} other{(participantCount > 2 ? "s" : "")}";
        }

        if (participantCount > 0)
            return $"{participantCount} participant{(participantCount != 1 ? "s" : "")} (no Director)";

        return "No participants assigned";
    }
}
