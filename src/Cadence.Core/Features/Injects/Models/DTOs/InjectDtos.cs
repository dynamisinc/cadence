using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Injects.Models.DTOs;

/// <summary>
/// DTO for inject response (read operations).
/// </summary>
public record InjectDto(
    Guid Id,
    int InjectNumber,
    string Title,
    string Description,
    TimeOnly ScheduledTime,
    int? ScenarioDay,
    TimeOnly? ScenarioTime,
    string Target,
    string? Source,
    DeliveryMethod? DeliveryMethod,
    InjectType InjectType,
    InjectStatus Status,
    int Sequence,
    Guid? ParentInjectId,
    string? TriggerCondition,
    string? ExpectedAction,
    string? ControllerNotes,
    DateTime? FiredAt,
    Guid? FiredBy,
    string? FiredByName,
    DateTime? SkippedAt,
    Guid? SkippedBy,
    string? SkippedByName,
    string? SkipReason,
    Guid MselId,
    Guid? PhaseId,
    string? PhaseName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for creating a new inject.
/// </summary>
public class CreateInjectRequest
{
    /// <summary>
    /// Brief descriptive name. Required, 3-200 characters.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Full inject content. Required, 1-4000 characters.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Planned delivery time (wall clock). Required.
    /// </summary>
    public TimeOnly ScheduledTime { get; init; }

    /// <summary>
    /// In-story day number (1-99). Optional.
    /// </summary>
    public int? ScenarioDay { get; init; }

    /// <summary>
    /// In-story time. Optional (but Day required if Time provided).
    /// </summary>
    public TimeOnly? ScenarioTime { get; init; }

    /// <summary>
    /// Player/role receiving the inject. Required, max 200 characters.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Simulated origin of the inject. Max 200 characters.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// How the inject is delivered to players.
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; init; }

    /// <summary>
    /// Type of inject (Standard, Contingency, Adaptive, Complexity).
    /// </summary>
    public InjectType InjectType { get; init; } = InjectType.Standard;

    /// <summary>
    /// Anticipated player response. Max 2000 characters.
    /// </summary>
    public string? ExpectedAction { get; init; }

    /// <summary>
    /// Private guidance for the Controller. Max 2000 characters.
    /// </summary>
    public string? ControllerNotes { get; init; }

    /// <summary>
    /// Parent inject for branching scenarios.
    /// </summary>
    public Guid? ParentInjectId { get; init; }

    /// <summary>
    /// Describes when to fire this branch inject. Max 500 characters.
    /// </summary>
    public string? TriggerCondition { get; init; }

    /// <summary>
    /// Exercise phase (optional).
    /// </summary>
    public Guid? PhaseId { get; init; }
}

/// <summary>
/// DTO for updating an existing inject.
/// </summary>
public class UpdateInjectRequest
{
    /// <summary>
    /// Brief descriptive name. Required, 3-200 characters.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Full inject content. Required, 1-4000 characters.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Planned delivery time (wall clock). Required.
    /// </summary>
    public TimeOnly ScheduledTime { get; init; }

    /// <summary>
    /// In-story day number (1-99). Optional.
    /// </summary>
    public int? ScenarioDay { get; init; }

    /// <summary>
    /// In-story time. Optional (but Day required if Time provided).
    /// </summary>
    public TimeOnly? ScenarioTime { get; init; }

    /// <summary>
    /// Player/role receiving the inject. Required, max 200 characters.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Simulated origin of the inject. Max 200 characters.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// How the inject is delivered to players.
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; init; }

    /// <summary>
    /// Type of inject (Standard, Contingency, Adaptive, Complexity).
    /// </summary>
    public InjectType InjectType { get; init; } = InjectType.Standard;

    /// <summary>
    /// Anticipated player response. Max 2000 characters.
    /// </summary>
    public string? ExpectedAction { get; init; }

    /// <summary>
    /// Private guidance for the Controller. Max 2000 characters.
    /// </summary>
    public string? ControllerNotes { get; init; }

    /// <summary>
    /// Parent inject for branching scenarios.
    /// </summary>
    public Guid? ParentInjectId { get; init; }

    /// <summary>
    /// Describes when to fire this branch inject. Max 500 characters.
    /// </summary>
    public string? TriggerCondition { get; init; }

    /// <summary>
    /// Exercise phase (optional).
    /// </summary>
    public Guid? PhaseId { get; init; }
}

/// <summary>
/// DTO for firing an inject.
/// </summary>
public class FireInjectRequest
{
    /// <summary>
    /// Optional notes about the firing (e.g., context or variations).
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// DTO for skipping an inject.
/// </summary>
public class SkipInjectRequest
{
    /// <summary>
    /// Reason for skipping. Required, max 500 characters.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Extension methods for mapping between Inject entity and DTOs.
/// </summary>
public static class InjectMapper
{
    public static InjectDto ToDto(this Inject entity) => new(
        entity.Id,
        entity.InjectNumber,
        entity.Title,
        entity.Description,
        entity.ScheduledTime,
        entity.ScenarioDay,
        entity.ScenarioTime,
        entity.Target,
        entity.Source,
        entity.DeliveryMethod,
        entity.InjectType,
        entity.Status,
        entity.Sequence,
        entity.ParentInjectId,
        entity.TriggerCondition,
        entity.ExpectedAction,
        entity.ControllerNotes,
        entity.FiredAt,
        entity.FiredBy,
        entity.FiredByUser?.DisplayName,
        entity.SkippedAt,
        entity.SkippedBy,
        entity.SkippedByUser?.DisplayName,
        entity.SkipReason,
        entity.MselId,
        entity.PhaseId,
        entity.Phase?.Name,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static Inject ToEntity(this CreateInjectRequest request, Guid mselId, int injectNumber, int sequence, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        InjectNumber = injectNumber,
        Title = request.Title,
        Description = request.Description,
        ScheduledTime = request.ScheduledTime,
        ScenarioDay = request.ScenarioDay,
        ScenarioTime = request.ScenarioTime,
        Target = request.Target,
        Source = request.Source,
        DeliveryMethod = request.DeliveryMethod,
        InjectType = request.InjectType,
        Status = InjectStatus.Pending,
        Sequence = sequence,
        ParentInjectId = request.ParentInjectId,
        TriggerCondition = request.TriggerCondition,
        ExpectedAction = request.ExpectedAction,
        ControllerNotes = request.ControllerNotes,
        MselId = mselId,
        PhaseId = request.PhaseId,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };

    public static void UpdateFromRequest(this Inject entity, UpdateInjectRequest request, Guid modifiedBy)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ScheduledTime = request.ScheduledTime;
        entity.ScenarioDay = request.ScenarioDay;
        entity.ScenarioTime = request.ScenarioTime;
        entity.Target = request.Target;
        entity.Source = request.Source;
        entity.DeliveryMethod = request.DeliveryMethod;
        entity.InjectType = request.InjectType;
        entity.ParentInjectId = request.ParentInjectId;
        entity.TriggerCondition = request.TriggerCondition;
        entity.ExpectedAction = request.ExpectedAction;
        entity.ControllerNotes = request.ControllerNotes;
        entity.PhaseId = request.PhaseId;
        entity.ModifiedBy = modifiedBy;
    }
}
