using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;

namespace DynamisReferenceApp.Api.Tools.Notes.Services;

/// <summary>
/// Service interface for notes operations.
/// </summary>
public interface INotesService
{
    /// <summary>
    /// Gets all notes for a user.
    /// </summary>
    Task<IEnumerable<NoteDto>> GetNotesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single note by ID.
    /// </summary>
    Task<NoteDto?> GetNoteAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new note.
    /// </summary>
    Task<NoteDto> CreateNoteAsync(CreateNoteRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    Task<NoteDto?> UpdateNoteAsync(Guid id, UpdateNoteRequest request, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a note.
    /// </summary>
    Task<bool> DeleteNoteAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted note.
    /// </summary>
    Task<NoteDto?> RestoreNoteAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
