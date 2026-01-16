using Cadence.Core.Data;
using Cadence.Core.Features.Objectives.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Objectives.Services;

/// <summary>
/// Service for objective operations.
/// </summary>
public class ObjectiveService : IObjectiveService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ObjectiveService> _logger;

    public ObjectiveService(
        AppDbContext context,
        ILogger<ObjectiveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObjectiveDto>> GetObjectivesByExerciseAsync(Guid exerciseId)
    {
        var objectives = await _context.Objectives
            .Include(o => o.InjectObjectives)
            .Where(o => o.ExerciseId == exerciseId)
            .OrderBy(o => o.ObjectiveNumber)
            .ToListAsync();

        return objectives.Select(o => o.ToDto(o.InjectObjectives.Count));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObjectiveSummaryDto>> GetObjectiveSummariesAsync(Guid exerciseId)
    {
        var objectives = await _context.Objectives
            .Where(o => o.ExerciseId == exerciseId)
            .OrderBy(o => o.ObjectiveNumber)
            .ToListAsync();

        return objectives.Select(o => o.ToSummaryDto());
    }

    /// <inheritdoc />
    public async Task<ObjectiveDto?> GetObjectiveAsync(Guid exerciseId, Guid id)
    {
        var objective = await _context.Objectives
            .Include(o => o.InjectObjectives)
            .FirstOrDefaultAsync(o => o.Id == id && o.ExerciseId == exerciseId);

        return objective?.ToDto(objective.InjectObjectives.Count);
    }

    /// <inheritdoc />
    public async Task<ObjectiveDto> CreateObjectiveAsync(Guid exerciseId, CreateObjectiveRequest request, Guid createdBy)
    {
        // Validate exercise exists
        var exercise = await _context.Exercises.FindAsync(exerciseId);
        if (exercise == null)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Check if exercise can be modified
        if (exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Archived exercises cannot be modified");
        }

        // Determine objective number
        string objectiveNumber;
        if (!string.IsNullOrWhiteSpace(request.ObjectiveNumber))
        {
            // Use provided number, check for duplicates
            if (!await IsObjectiveNumberUniqueAsync(exerciseId, request.ObjectiveNumber))
            {
                throw new InvalidOperationException($"Objective number '{request.ObjectiveNumber}' already exists");
            }
            objectiveNumber = request.ObjectiveNumber;
        }
        else
        {
            // Auto-assign next sequential number
            objectiveNumber = await GetNextObjectiveNumberAsync(exerciseId);
        }

        var objective = request.ToEntity(exerciseId, objectiveNumber, createdBy);

        _context.Objectives.Add(objective);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created objective {ObjectiveId}: {ObjectiveName} for exercise {ExerciseId}",
            objective.Id, objective.Name, exerciseId);

        return objective.ToDto(0);
    }

    /// <inheritdoc />
    public async Task<ObjectiveDto?> UpdateObjectiveAsync(Guid exerciseId, Guid id, UpdateObjectiveRequest request, Guid modifiedBy)
    {
        var objective = await _context.Objectives
            .Include(o => o.InjectObjectives)
            .Include(o => o.Exercise)
            .FirstOrDefaultAsync(o => o.Id == id && o.ExerciseId == exerciseId);

        if (objective == null)
        {
            return null;
        }

        // Check if exercise can be modified
        if (objective.Exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Archived exercises cannot be modified");
        }

        // Check for duplicate objective number if changed
        if (objective.ObjectiveNumber != request.ObjectiveNumber)
        {
            if (!await IsObjectiveNumberUniqueAsync(exerciseId, request.ObjectiveNumber, id))
            {
                throw new InvalidOperationException($"Objective number '{request.ObjectiveNumber}' already exists");
            }
        }

        objective.UpdateFromRequest(request, modifiedBy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated objective {ObjectiveId}: {ObjectiveName}", id, objective.Name);

        return objective.ToDto(objective.InjectObjectives.Count);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteObjectiveAsync(Guid exerciseId, Guid id, Guid deletedBy)
    {
        var objective = await _context.Objectives
            .Include(o => o.InjectObjectives)
            .Include(o => o.Exercise)
            .FirstOrDefaultAsync(o => o.Id == id && o.ExerciseId == exerciseId);

        if (objective == null)
        {
            return false;
        }

        // Check if exercise can be modified
        if (objective.Exercise.Status == ExerciseStatus.Archived)
        {
            throw new InvalidOperationException("Archived exercises cannot be modified");
        }

        // Check if objective has linked injects
        if (objective.InjectObjectives.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete objective with {objective.InjectObjectives.Count} linked inject(s). Remove the links first.");
        }

        // Soft delete
        objective.IsDeleted = true;
        objective.DeletedAt = DateTime.UtcNow;
        objective.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted objective {ObjectiveId}: {ObjectiveName}", id, objective.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsObjectiveNumberUniqueAsync(Guid exerciseId, string objectiveNumber, Guid? excludeId = null)
    {
        var query = _context.Objectives
            .Where(o => o.ExerciseId == exerciseId && o.ObjectiveNumber == objectiveNumber);

        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Get the next sequential objective number for an exercise.
    /// </summary>
    private async Task<string> GetNextObjectiveNumberAsync(Guid exerciseId)
    {
        var maxNumber = await _context.Objectives
            .Where(o => o.ExerciseId == exerciseId)
            .Select(o => o.ObjectiveNumber)
            .ToListAsync();

        // Try to find the highest numeric value
        int highestNumeric = 0;
        foreach (var num in maxNumber)
        {
            if (int.TryParse(num, out int parsed) && parsed > highestNumeric)
            {
                highestNumeric = parsed;
            }
        }

        return (highestNumeric + 1).ToString();
    }
}
