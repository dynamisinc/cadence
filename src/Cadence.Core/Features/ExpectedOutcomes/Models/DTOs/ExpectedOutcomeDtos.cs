using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.ExpectedOutcomes.Models.DTOs;

/// <summary>
/// DTO for expected outcome response (read operations).
/// </summary>
public record ExpectedOutcomeDto(
    Guid Id,
    Guid InjectId,
    string Description,
    int SortOrder,
    bool? WasAchieved,
    string? EvaluatorNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for creating a new expected outcome.
/// </summary>
public class CreateExpectedOutcomeRequest
{
    /// <summary>
    /// Description of the expected outcome. Required, 1-1000 characters.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Display order. If not specified, appended to end.
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// DTO for updating an existing expected outcome.
/// </summary>
public class UpdateExpectedOutcomeRequest
{
    /// <summary>
    /// Description of the expected outcome. Required, 1-1000 characters.
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// DTO for evaluating an expected outcome during AAR.
/// </summary>
public class EvaluateExpectedOutcomeRequest
{
    /// <summary>
    /// Whether the outcome was achieved. Null to clear evaluation.
    /// </summary>
    public bool? WasAchieved { get; init; }

    /// <summary>
    /// Evaluator's notes. Max 2000 characters.
    /// </summary>
    public string? EvaluatorNotes { get; init; }
}

/// <summary>
/// DTO for reordering expected outcomes.
/// </summary>
public class ReorderExpectedOutcomesRequest
{
    /// <summary>
    /// List of outcome IDs in the new order.
    /// </summary>
    public List<Guid> OutcomeIds { get; init; } = new();
}

/// <summary>
/// Extension methods for mapping between ExpectedOutcome entity and DTOs.
/// </summary>
public static class ExpectedOutcomeMapper
{
    public static ExpectedOutcomeDto ToDto(this ExpectedOutcome entity) => new(
        entity.Id,
        entity.InjectId,
        entity.Description,
        entity.SortOrder,
        entity.WasAchieved,
        entity.EvaluatorNotes,
        entity.CreatedAt,
        entity.UpdatedAt
    );

    public static ExpectedOutcome ToEntity(this CreateExpectedOutcomeRequest request, Guid injectId, int sortOrder, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(),
        InjectId = injectId,
        Description = request.Description,
        SortOrder = request.SortOrder ?? sortOrder,
        CreatedBy = createdBy,
        ModifiedBy = createdBy
    };

    public static void UpdateFromRequest(this ExpectedOutcome entity, UpdateExpectedOutcomeRequest request, Guid modifiedBy)
    {
        entity.Description = request.Description;
        entity.ModifiedBy = modifiedBy;
    }

    public static void EvaluateFromRequest(this ExpectedOutcome entity, EvaluateExpectedOutcomeRequest request, Guid modifiedBy)
    {
        entity.WasAchieved = request.WasAchieved;
        entity.EvaluatorNotes = request.EvaluatorNotes;
        entity.ModifiedBy = modifiedBy;
    }
}
