using Cadence.Core.Data;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Observations.Services;

/// <summary>
/// Service for observation operations.
/// </summary>
public class ObservationService : IObservationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ObservationService> _logger;

    public ObservationService(AppDbContext context, ILogger<ObservationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObservationDto>> GetObservationsByExerciseAsync(Guid exerciseId)
    {
        var observations = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .Where(o => o.ExerciseId == exerciseId)
            .OrderByDescending(o => o.ObservedAt)
            .ToListAsync();

        return observations.Select(o => o.ToDto());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ObservationDto>> GetObservationsByInjectAsync(Guid injectId)
    {
        var observations = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .Where(o => o.InjectId == injectId)
            .OrderByDescending(o => o.ObservedAt)
            .ToListAsync();

        return observations.Select(o => o.ToDto());
    }

    /// <inheritdoc />
    public async Task<ObservationDto?> GetObservationAsync(Guid id)
    {
        var observation = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .FirstOrDefaultAsync(o => o.Id == id);

        return observation?.ToDto();
    }

    /// <inheritdoc />
    public async Task<ObservationDto> CreateObservationAsync(Guid exerciseId, CreateObservationRequest request, Guid createdBy)
    {
        // Validate exercise exists
        var exerciseExists = await _context.Exercises.AnyAsync(e => e.Id == exerciseId);
        if (!exerciseExists)
        {
            throw new InvalidOperationException($"Exercise {exerciseId} not found");
        }

        // Validate inject exists if specified
        if (request.InjectId.HasValue)
        {
            var injectExists = await _context.Injects.AnyAsync(i => i.Id == request.InjectId.Value);
            if (!injectExists)
            {
                throw new InvalidOperationException($"Inject {request.InjectId.Value} not found");
            }
        }

        // Validate objective exists if specified
        if (request.ObjectiveId.HasValue)
        {
            var objectiveExists = await _context.Objectives.AnyAsync(o => o.Id == request.ObjectiveId.Value);
            if (!objectiveExists)
            {
                throw new InvalidOperationException($"Objective {request.ObjectiveId.Value} not found");
            }
        }

        var observation = request.ToEntity(exerciseId, createdBy);

        _context.Observations.Add(observation);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created observation {ObservationId} for exercise {ExerciseId}",
            observation.Id, exerciseId);

        // Reload with navigation properties
        await _context.Entry(observation)
            .Reference(o => o.CreatedByUser)
            .LoadAsync();
        await _context.Entry(observation)
            .Reference(o => o.Inject)
            .LoadAsync();

        return observation.ToDto();
    }

    /// <inheritdoc />
    public async Task<ObservationDto?> UpdateObservationAsync(Guid id, UpdateObservationRequest request, Guid modifiedBy)
    {
        var observation = await _context.Observations
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (observation == null)
        {
            return null;
        }

        // Validate inject exists if specified
        if (request.InjectId.HasValue)
        {
            var injectExists = await _context.Injects.AnyAsync(i => i.Id == request.InjectId.Value);
            if (!injectExists)
            {
                throw new InvalidOperationException($"Inject {request.InjectId.Value} not found");
            }
        }

        // Validate objective exists if specified
        if (request.ObjectiveId.HasValue)
        {
            var objectiveExists = await _context.Objectives.AnyAsync(o => o.Id == request.ObjectiveId.Value);
            if (!objectiveExists)
            {
                throw new InvalidOperationException($"Objective {request.ObjectiveId.Value} not found");
            }
        }

        // Update fields
        observation.Content = request.Content;
        observation.Rating = request.Rating;
        observation.Recommendation = request.Recommendation;
        observation.Location = request.Location;
        observation.InjectId = request.InjectId;
        observation.ObjectiveId = request.ObjectiveId;
        observation.ModifiedBy = modifiedBy;

        if (request.ObservedAt.HasValue)
        {
            observation.ObservedAt = request.ObservedAt.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated observation {ObservationId}", id);

        // Reload inject navigation if it changed
        await _context.Entry(observation)
            .Reference(o => o.Inject)
            .LoadAsync();

        return observation.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteObservationAsync(Guid id, Guid deletedBy)
    {
        var observation = await _context.Observations.FindAsync(id);

        if (observation == null)
        {
            return false;
        }

        // Soft delete
        observation.IsDeleted = true;
        observation.DeletedAt = DateTime.UtcNow;
        observation.DeletedBy = deletedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted observation {ObservationId}", id);

        return true;
    }
}
