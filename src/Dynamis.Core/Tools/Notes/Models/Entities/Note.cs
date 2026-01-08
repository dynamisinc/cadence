using DynamisReferenceApp.Api.Core.Data;

namespace DynamisReferenceApp.Api.Tools.Notes.Models.Entities;

/// <summary>
/// Note entity - demonstrates a typical CRUD entity with soft delete.
/// </summary>
public class Note : IHasTimestamps
{
    /// <summary>
    /// Unique identifier for the note.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID of the note owner.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Note title (required, max 100 characters).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Note content (optional, max 10000 characters).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// When the note was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the note was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete flag.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// When the note was soft deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
