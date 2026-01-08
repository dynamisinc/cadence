using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;
using DynamisReferenceApp.Api.Tools.Notes.Models.Entities;

namespace DynamisReferenceApp.Api.Tools.Notes.Mappers;

/// <summary>
/// Maps between Note entities and DTOs.
/// </summary>
public static class NoteMapper
{
    /// <summary>
    /// Maps a Note entity to a NoteDto.
    /// </summary>
    public static NoteDto ToDto(this Note note)
    {
        return new NoteDto(
            Id: note.Id,
            Title: note.Title,
            Content: note.Content,
            CreatedAt: note.CreatedAt,
            UpdatedAt: note.UpdatedAt
        );
    }

    /// <summary>
    /// Maps a collection of Note entities to DTOs.
    /// </summary>
    public static IEnumerable<NoteDto> ToDtos(this IEnumerable<Note> notes)
    {
        return notes.Select(n => n.ToDto());
    }

    /// <summary>
    /// Creates a new Note entity from a CreateNoteRequest.
    /// </summary>
    public static Note ToEntity(this CreateNoteRequest request, string userId)
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title.Trim(),
            Content = request.Content?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Updates an existing Note entity from an UpdateNoteRequest.
    /// </summary>
    public static void UpdateFrom(this Note note, UpdateNoteRequest request)
    {
        note.Title = request.Title.Trim();
        note.Content = request.Content?.Trim();
        note.UpdatedAt = DateTime.UtcNow;
    }
}
