using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Objectives.Models.DTOs;

/// <summary>
/// DTO for creating a new objective.
/// </summary>
public class CreateObjectiveRequest
{
    /// <summary>
    /// Objective number for display ordering (e.g., "1", "2", "1.1", "A").
    /// Optional - auto-assigned if blank. Max 10 characters.
    /// </summary>
    public string? ObjectiveNumber { get; init; }

    /// <summary>
    /// Short objective name. Required, 3-200 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description of the objective. Max 2000 characters.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// DTO for updating an existing objective.
/// </summary>
public class UpdateObjectiveRequest
{
    /// <summary>
    /// Objective number for display ordering. Max 10 characters.
    /// </summary>
    public string ObjectiveNumber { get; init; } = string.Empty;

    /// <summary>
    /// Short objective name. Required, 3-200 characters.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description of the objective. Max 2000 characters.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// DTO for objective response.
/// </summary>
public record ObjectiveDto(
    Guid Id,
    string ObjectiveNumber,
    string Name,
    string? Description,
    Guid ExerciseId,
    int LinkedInjectCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Lightweight DTO for objective selection dropdowns.
/// Includes description for tooltip display.
/// </summary>
public record ObjectiveSummaryDto(
    Guid Id,
    string ObjectiveNumber,
    string Name,
    string? Description
);

/// <summary>
/// Extension methods for mapping between Objective entity and DTOs.
/// </summary>
public static class ObjectiveMapper
{
    public static ObjectiveDto ToDto(this Objective entity, int linkedInjectCount = 0) => new(
        entity.Id,
        entity.ObjectiveNumber,
        entity.Name,
        entity.Description,
        entity.ExerciseId,
        linkedInjectCount,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static ObjectiveSummaryDto ToSummaryDto(this Objective entity) => new(
        entity.Id,
        entity.ObjectiveNumber,
        entity.Name,
        entity.Description
    );

    public static Objective ToEntity(this CreateObjectiveRequest request, Guid exerciseId, string objectiveNumber, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        ExerciseId = exerciseId,
        ObjectiveNumber = objectiveNumber,
        Name = request.Name,
        Description = request.Description,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };

    public static void UpdateFromRequest(this Objective entity, UpdateObjectiveRequest request, Guid modifiedBy)
    {
        entity.ObjectiveNumber = request.ObjectiveNumber;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ModifiedBy = modifiedBy;
    }
}
