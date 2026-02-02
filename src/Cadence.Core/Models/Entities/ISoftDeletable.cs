namespace Cadence.Core.Models.Entities;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft-deleted entities are marked as deleted rather than physically removed.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates whether this entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// The UTC timestamp when this entity was soft-deleted.
    /// Null if not deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The ID of the ApplicationUser who deleted this entity.
    /// Null if not deleted.
    /// </summary>
    string? DeletedBy { get; set; }
}
