using Cadence.Core.Data;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Base class for all user-created entities in the Cadence domain.
/// Provides common audit fields for tracking creation, modification, and soft deletion.
/// </summary>
public abstract class BaseEntity : IHasTimestamps, ISoftDeletable
{
    /// <summary>
    /// Unique identifier for this entity.
    /// </summary>
    public Guid Id { get; set; }

    // =========================================================================
    // IHasTimestamps - Set automatically by DbContext.SaveChanges()
    // =========================================================================

    /// <summary>
    /// UTC timestamp when this entity was created.
    /// Set automatically on insert.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified.
    /// Set automatically on insert and update.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The ID of the user who created this entity.
    /// Uses SystemUserId when auth not available.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// The ID of the user who last modified this entity.
    /// Uses SystemUserId when auth not available.
    /// </summary>
    public Guid ModifiedBy { get; set; }

    // =========================================================================
    // ISoftDeletable - Use soft delete for all user data
    // =========================================================================

    /// <summary>
    /// Indicates whether this entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was soft-deleted.
    /// Null if not deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// The ID of the user who deleted this entity.
    /// Null if not deleted.
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
