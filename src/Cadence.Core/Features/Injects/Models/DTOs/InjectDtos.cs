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
    TimeSpan? DeliveryTime,
    int? ScenarioDay,
    TimeOnly? ScenarioTime,
    string Target,
    string? Source,
    // Legacy enum - kept for backward compatibility during migration
    DeliveryMethod? DeliveryMethod,
    // New lookup-based delivery method
    Guid? DeliveryMethodId,
    string? DeliveryMethodName,
    string? DeliveryMethodOther,
    InjectType InjectType,
    InjectStatus Status,
    int Sequence,
    Guid? ParentInjectId,
    string? TriggerCondition,
    string? ExpectedAction,
    string? ControllerNotes,
    DateTime? ReadyAt,
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
    List<Guid> ObjectiveIds,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    // New Phase G fields
    string? SourceReference,
    int? Priority,
    TriggerType TriggerType,
    string? ResponsibleController,
    string? LocationName,
    string? LocationType,
    string? Track
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
    /// Elapsed time from exercise start. Format: "HH:MM:SS"
    /// </summary>
    public TimeSpan? DeliveryTime { get; init; }

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
    /// How the inject is delivered to players (legacy enum).
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; init; }

    /// <summary>
    /// Delivery method lookup ID (preferred over enum).
    /// </summary>
    public Guid? DeliveryMethodId { get; init; }

    /// <summary>
    /// Custom delivery method text (when "Other" is selected).
    /// </summary>
    public string? DeliveryMethodOther { get; init; }

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

    /// <summary>
    /// Linked objective IDs (many-to-many).
    /// </summary>
    public List<Guid>? ObjectiveIds { get; init; }

    // =========================================================================
    // New Phase G Fields
    // =========================================================================

    /// <summary>
    /// Original inject ID from imported file for traceability. Max 50 characters.
    /// </summary>
    public string? SourceReference { get; init; }

    /// <summary>
    /// Priority level (1=Critical, 2=High, 3=Medium, 4=Low, 5=Informational).
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// How the inject is triggered (Manual, Scheduled, Conditional).
    /// </summary>
    public TriggerType TriggerType { get; init; } = TriggerType.Manual;

    /// <summary>
    /// Name/role of the controller responsible for firing this inject. Max 200 characters.
    /// </summary>
    public string? ResponsibleController { get; init; }

    /// <summary>
    /// Venue/site name where inject takes place. Max 200 characters.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Type of location (EOC, Hospital, Stadium, Field, etc.). Max 100 characters.
    /// </summary>
    public string? LocationType { get; init; }

    /// <summary>
    /// Agency grouping for multi-agency exercises (LAFD, LAPD, Venue, EOC). Max 100 characters.
    /// </summary>
    public string? Track { get; init; }
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
    /// Elapsed time from exercise start. Format: "HH:MM:SS"
    /// </summary>
    public TimeSpan? DeliveryTime { get; init; }

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
    /// How the inject is delivered to players (legacy enum).
    /// </summary>
    public DeliveryMethod? DeliveryMethod { get; init; }

    /// <summary>
    /// Delivery method lookup ID (preferred over enum).
    /// </summary>
    public Guid? DeliveryMethodId { get; init; }

    /// <summary>
    /// Custom delivery method text (when "Other" is selected).
    /// </summary>
    public string? DeliveryMethodOther { get; init; }

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

    /// <summary>
    /// Linked objective IDs (many-to-many).
    /// </summary>
    public List<Guid>? ObjectiveIds { get; init; }

    // =========================================================================
    // New Phase G Fields
    // =========================================================================

    /// <summary>
    /// Original inject ID from imported file for traceability. Max 50 characters.
    /// </summary>
    public string? SourceReference { get; init; }

    /// <summary>
    /// Priority level (1=Critical, 2=High, 3=Medium, 4=Low, 5=Informational).
    /// </summary>
    public int? Priority { get; init; }

    /// <summary>
    /// How the inject is triggered (Manual, Scheduled, Conditional).
    /// </summary>
    public TriggerType TriggerType { get; init; } = TriggerType.Manual;

    /// <summary>
    /// Name/role of the controller responsible for firing this inject. Max 200 characters.
    /// </summary>
    public string? ResponsibleController { get; init; }

    /// <summary>
    /// Venue/site name where inject takes place. Max 200 characters.
    /// </summary>
    public string? LocationName { get; init; }

    /// <summary>
    /// Type of location (EOC, Hospital, Stadium, Field, etc.). Max 100 characters.
    /// </summary>
    public string? LocationType { get; init; }

    /// <summary>
    /// Agency grouping for multi-agency exercises (LAFD, LAPD, Venue, EOC). Max 100 characters.
    /// </summary>
    public string? Track { get; init; }
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
/// DTO for reordering injects.
/// </summary>
public class ReorderInjectsRequest
{
    /// <summary>
    /// Ordered list of inject IDs representing the new sequence.
    /// </summary>
    public List<Guid> InjectIds { get; init; } = new();
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
        entity.DeliveryTime,
        entity.ScenarioDay,
        entity.ScenarioTime,
        entity.Target,
        entity.Source,
        entity.DeliveryMethod,
        entity.DeliveryMethodId,
        entity.DeliveryMethodLookup?.Name,
        entity.DeliveryMethodOther,
        entity.InjectType,
        entity.Status,
        entity.Sequence,
        entity.ParentInjectId,
        entity.FireCondition,
        entity.ExpectedAction,
        entity.ControllerNotes,
        entity.ReadyAt,
        entity.FiredAt,
        // Parse string ApplicationUser.Id to Guid for DTO backward compatibility
        string.IsNullOrEmpty(entity.FiredByUserId) ? null : Guid.Parse(entity.FiredByUserId),
        entity.FiredByUser?.DisplayName,
        entity.SkippedAt,
        string.IsNullOrEmpty(entity.SkippedByUserId) ? null : Guid.Parse(entity.SkippedByUserId),
        entity.SkippedByUser?.DisplayName,
        entity.SkipReason,
        entity.MselId,
        entity.PhaseId,
        entity.Phase?.Name,
        entity.InjectObjectives?.Select(io => io.ObjectiveId).ToList() ?? new List<Guid>(),
        entity.CreatedAt,
        entity.UpdatedAt,
        // New Phase G fields
        entity.SourceReference,
        entity.Priority,
        entity.TriggerType,
        entity.ResponsibleController,
        entity.LocationName,
        entity.LocationType,
        entity.Track
    );

    public static Inject ToEntity(this CreateInjectRequest request, Guid mselId, int injectNumber, int sequence, string createdBy) => new()
    {
        Id = Guid.NewGuid(),
        InjectNumber = injectNumber,
        Title = request.Title,
        Description = request.Description,
        ScheduledTime = request.ScheduledTime,
        DeliveryTime = request.DeliveryTime,
        ScenarioDay = request.ScenarioDay,
        ScenarioTime = request.ScenarioTime,
        Target = request.Target,
        Source = request.Source,
        DeliveryMethod = request.DeliveryMethod,
        DeliveryMethodId = request.DeliveryMethodId,
        DeliveryMethodOther = request.DeliveryMethodOther,
        InjectType = request.InjectType,
        Status = InjectStatus.Draft,
        Sequence = sequence,
        ParentInjectId = request.ParentInjectId,
        FireCondition = request.TriggerCondition,
        ExpectedAction = request.ExpectedAction,
        ControllerNotes = request.ControllerNotes,
        MselId = mselId,
        PhaseId = request.PhaseId,
        // New Phase G fields
        SourceReference = request.SourceReference,
        Priority = request.Priority,
        TriggerType = request.TriggerType,
        ResponsibleController = request.ResponsibleController,
        LocationName = request.LocationName,
        LocationType = request.LocationType,
        Track = request.Track,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };

    public static void UpdateFromRequest(this Inject entity, UpdateInjectRequest request, string modifiedBy)
    {
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ScheduledTime = request.ScheduledTime;
        entity.DeliveryTime = request.DeliveryTime;
        entity.ScenarioDay = request.ScenarioDay;
        entity.ScenarioTime = request.ScenarioTime;
        entity.Target = request.Target;
        entity.Source = request.Source;
        entity.DeliveryMethod = request.DeliveryMethod;
        entity.DeliveryMethodId = request.DeliveryMethodId;
        entity.DeliveryMethodOther = request.DeliveryMethodOther;
        entity.InjectType = request.InjectType;
        entity.ParentInjectId = request.ParentInjectId;
        entity.FireCondition = request.TriggerCondition;
        entity.ExpectedAction = request.ExpectedAction;
        entity.ControllerNotes = request.ControllerNotes;
        entity.PhaseId = request.PhaseId;
        // New Phase G fields
        entity.SourceReference = request.SourceReference;
        entity.Priority = request.Priority;
        entity.TriggerType = request.TriggerType;
        entity.ResponsibleController = request.ResponsibleController;
        entity.LocationName = request.LocationName;
        entity.LocationType = request.LocationType;
        entity.Track = request.Track;
        entity.ModifiedBy = modifiedBy;
    }
}
