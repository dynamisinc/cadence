using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Exercises.Models.DTOs;

/// <summary>
/// DTO for creating a new exercise (minimal required fields).
/// </summary>
public class CreateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public bool IsPracticeMode { get; init; }
}

/// <summary>
/// DTO for updating an existing exercise.
/// </summary>
public class UpdateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public ExerciseType ExerciseType { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public bool IsPracticeMode { get; init; }
}

/// <summary>
/// DTO for duplicating an exercise.
/// Optional fields can override the source exercise values.
/// </summary>
public class DuplicateExerciseRequest
{
    /// <summary>
    /// Name for the new exercise. Defaults to "Copy of {original name}".
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Scheduled date for the new exercise. Defaults to the original date.
    /// </summary>
    public DateOnly? ScheduledDate { get; init; }
}

/// <summary>
/// DTO for exercise response.
/// </summary>
public record ExerciseDto(
    Guid Id,
    string Name,
    string? Description,
    ExerciseType ExerciseType,
    ExerciseStatus Status,
    bool IsPracticeMode,
    DateOnly ScheduledDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string TimeZoneId,
    string? Location,
    Guid OrganizationId,
    Guid? ActiveMselId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Extension methods for mapping between Exercise entity and DTOs.
/// </summary>
public static class ExerciseMapper
{
    public static ExerciseDto ToDto(this Exercise entity) => new(
        entity.Id,
        entity.Name,
        entity.Description,
        entity.ExerciseType,
        entity.Status,
        entity.IsPracticeMode,
        entity.ScheduledDate,
        entity.StartTime,
        entity.EndTime,
        entity.TimeZoneId,
        entity.Location,
        entity.OrganizationId,
        entity.ActiveMselId,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static Exercise ToEntity(this CreateExerciseRequest request, Guid organizationId, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Description = request.Description,
        ExerciseType = request.ExerciseType,
        Status = ExerciseStatus.Draft,
        IsPracticeMode = request.IsPracticeMode,
        ScheduledDate = request.ScheduledDate,
        TimeZoneId = request.TimeZoneId,
        Location = request.Location,
        OrganizationId = organizationId,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };
}
