using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Phases.Models.DTOs;

/// <summary>
/// DTO for phase response (read operations).
/// </summary>
public record PhaseDto(
    Guid Id,
    string Name,
    string? Description,
    int Sequence,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    Guid ExerciseId,
    int InjectCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for creating a new phase.
/// </summary>
public class CreatePhaseRequest
{
    /// <summary>
    /// Phase name. Required, 3-100 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Phase description. Optional, max 500 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Phase start time (wall clock). Optional.
    /// </summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>
    /// Phase end time (wall clock). Optional.
    /// </summary>
    public TimeOnly? EndTime { get; init; }
}

/// <summary>
/// DTO for updating an existing phase.
/// </summary>
public class UpdatePhaseRequest
{
    /// <summary>
    /// Phase name. Required, 3-100 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Phase description. Optional, max 500 characters.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Phase start time (wall clock). Optional.
    /// </summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>
    /// Phase end time (wall clock). Optional.
    /// </summary>
    public TimeOnly? EndTime { get; init; }
}

/// <summary>
/// DTO for reordering phases.
/// </summary>
public class ReorderPhasesRequest
{
    /// <summary>
    /// List of phase IDs in desired order.
    /// </summary>
    public List<Guid> PhaseIds { get; init; } = new();
}

/// <summary>
/// Extension methods for mapping between Phase entity and DTOs.
/// </summary>
public static class PhaseMapper
{
    public static PhaseDto ToDto(this Phase entity, int injectCount = 0) => new(
        entity.Id,
        entity.Name,
        entity.Description,
        entity.Sequence,
        entity.StartTime,
        entity.EndTime,
        entity.ExerciseId,
        injectCount,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static Phase ToEntity(this CreatePhaseRequest request, Guid exerciseId, int sequence, string createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description,
        Sequence = sequence,
        StartTime = request.StartTime,
        EndTime = request.EndTime,
        ExerciseId = exerciseId,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };

    public static void UpdateFromRequest(this Phase entity, UpdatePhaseRequest request, string modifiedBy)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.ModifiedBy = modifiedBy;
    }
}
