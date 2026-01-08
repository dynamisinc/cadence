namespace DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;

/// <summary>
/// Data transfer object for a note.
/// </summary>
public record NoteDto(
    Guid Id,
    string Title,
    string? Content,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// Request to create a new note.
/// </summary>
public record CreateNoteRequest(
    string Title,
    string? Content
)
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title is required.", nameof(Title));

        if (Title.Length > 100)
            throw new ArgumentException("Title must be 100 characters or less.", nameof(Title));

        if (Content?.Length > 10000)
            throw new ArgumentException("Content must be 10,000 characters or less.", nameof(Content));
    }
}

/// <summary>
/// Request to update an existing note.
/// </summary>
public record UpdateNoteRequest(
    string Title,
    string? Content
)
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title is required.", nameof(Title));

        if (Title.Length > 100)
            throw new ArgumentException("Title must be 100 characters or less.", nameof(Title));

        if (Content?.Length > 10000)
            throw new ArgumentException("Content must be 10,000 characters or less.", nameof(Content));
    }
}
