using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Observations.Models.DTOs;

/// <summary>
/// DTO for creating a new observation.
/// </summary>
public class CreateObservationRequest
{
    /// <summary>
    /// The observation content. Required, 1-4000 characters.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// HSEEP P/S/M/U performance rating. Optional.
    /// </summary>
    public ObservationRating? Rating { get; init; }

    /// <summary>
    /// Evaluator's recommendation. Max 2000 characters.
    /// </summary>
    public string? Recommendation { get; init; }

    /// <summary>
    /// When the observation was made. Defaults to current time if not specified.
    /// </summary>
    public DateTime? ObservedAt { get; init; }

    /// <summary>
    /// Physical or functional location. Max 200 characters.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// The inject this observation relates to. Optional.
    /// </summary>
    public Guid? InjectId { get; init; }

    /// <summary>
    /// The objective this observation relates to. Optional.
    /// </summary>
    public Guid? ObjectiveId { get; init; }

    /// <summary>
    /// Capability IDs to tag this observation with. Optional.
    /// </summary>
    public List<Guid>? CapabilityIds { get; init; }
}

/// <summary>
/// DTO for updating an existing observation.
/// </summary>
public class UpdateObservationRequest
{
    /// <summary>
    /// The observation content. Required, 1-4000 characters.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// HSEEP P/S/M/U performance rating. Optional.
    /// </summary>
    public ObservationRating? Rating { get; init; }

    /// <summary>
    /// Evaluator's recommendation. Max 2000 characters.
    /// </summary>
    public string? Recommendation { get; init; }

    /// <summary>
    /// When the observation was made.
    /// </summary>
    public DateTime? ObservedAt { get; init; }

    /// <summary>
    /// Physical or functional location. Max 200 characters.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// The inject this observation relates to. Optional.
    /// </summary>
    public Guid? InjectId { get; init; }

    /// <summary>
    /// The objective this observation relates to. Optional.
    /// </summary>
    public Guid? ObjectiveId { get; init; }

    /// <summary>
    /// Capability IDs to tag this observation with. Optional.
    /// If provided (including empty list), replaces existing capability links.
    /// If null, existing capability links are preserved.
    /// </summary>
    public List<Guid>? CapabilityIds { get; init; }
}

/// <summary>
/// DTO for observation response.
/// </summary>
public record ObservationDto(
    Guid Id,
    Guid ExerciseId,
    Guid? InjectId,
    Guid? ObjectiveId,
    string Content,
    ObservationRating? Rating,
    string? Recommendation,
    DateTime ObservedAt,
    string? Location,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid? CreatedBy,
    string? CreatedByName,
    string? InjectTitle,
    int? InjectNumber,
    List<CapabilityTagDto> Capabilities
);

/// <summary>
/// DTO for capability tags on observations.
/// </summary>
public record CapabilityTagDto(
    Guid Id,
    string Name,
    string? Category
);

/// <summary>
/// Extension methods for mapping between Observation entity and DTOs.
/// </summary>
public static class ObservationMapper
{
    public static ObservationDto ToDto(this Observation entity) => new(
        entity.Id,
        entity.ExerciseId,
        entity.InjectId,
        entity.ObjectiveId,
        entity.Content,
        entity.Rating,
        entity.Recommendation,
        entity.ObservedAt,
        entity.Location,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.CreatedBy,
        entity.CreatedByUser?.DisplayName,
        entity.Inject?.Title,
        entity.Inject?.InjectNumber,
        entity.ObservationCapabilities
            .Select(oc => new CapabilityTagDto(
                oc.Capability.Id,
                oc.Capability.Name,
                oc.Capability.Category))
            .ToList()
    );

    public static Observation ToEntity(this CreateObservationRequest request, Guid exerciseId, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        ExerciseId = exerciseId,
        InjectId = request.InjectId,
        ObjectiveId = request.ObjectiveId,
        Content = request.Content,
        Rating = request.Rating,
        Recommendation = request.Recommendation,
        ObservedAt = request.ObservedAt ?? DateTime.UtcNow,
        Location = request.Location,
        // Set both the string FK for ApplicationUser and the Guid for BaseEntity audit
        CreatedByUserId = createdBy.ToString(),
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };
}
