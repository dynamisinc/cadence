using System.Diagnostics;
using FluentValidation;
using Cadence.Api.Tools.Notes.Mappers;
using Cadence.Api.Tools.Notes.Models.DTOs;
using Cadence.Api.Tools.Notes.Models.Entities;

namespace Cadence.Api.Tools.Notes.Services;

/// <summary>
/// Service implementation for notes operations.
/// </summary>
public class NotesService : INotesService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotesService> _logger;
    private readonly IValidator<CreateNoteRequest> _createValidator;
    private readonly IValidator<UpdateNoteRequest> _updateValidator;

    public NotesService(
        AppDbContext context,
        ILogger<NotesService> logger,
        IValidator<CreateNoteRequest> createValidator,
        IValidator<UpdateNoteRequest> updateValidator)
    {
        _context = context;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NoteDto>> GetNotesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("GetNotes", userId: userId);
        var sw = Stopwatch.StartNew();

        var notes = await _context.Notes
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.UpdatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        sw.Stop();
        _logger.LogOperationSuccess("GetNotes", durationMs: sw.ElapsedMilliseconds);
        _logger.LogSlowOperation("GetNotes", sw.ElapsedMilliseconds);

        return notes.ToDtos();
    }

    /// <inheritdoc />
    public async Task<NoteDto?> GetNoteAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("GetNote", entityId: id.ToString(), userId: userId);

        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (note == null)
        {
            _logger.LogEntityNotFound<Note>(id.ToString());
            return null;
        }

        _logger.LogOperationSuccess("GetNote", entityId: id.ToString());
        return note.ToDto();
    }

    /// <inheritdoc />
    public async Task<NoteDto> CreateNoteAsync(
        CreateNoteRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("CreateNote", userId: userId);

        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var note = request.ToEntity(userId);

        _context.Notes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogEntityCreated<Note>(note.Id.ToString(), userId);

        return note.ToDto();
    }

    /// <inheritdoc />
    public async Task<NoteDto?> UpdateNoteAsync(
        Guid id,
        UpdateNoteRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("UpdateNote", entityId: id.ToString(), userId: userId);

        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (note == null)
        {
            _logger.LogEntityNotFound<Note>(id.ToString());
            return null;
        }

        note.UpdateFrom(request);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogEntityUpdated<Note>(id.ToString(), userId);

        return note.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteNoteAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("DeleteNote", entityId: id.ToString(), userId: userId);

        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId && !n.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (note == null)
        {
            _logger.LogEntityNotFound<Note>(id.ToString());
            return false;
        }

        // Soft delete
        note.IsDeleted = true;
        note.DeletedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogEntityDeleted<Note>(id.ToString(), userId);

        return true;
    }

    /// <inheritdoc />
    public async Task<NoteDto?> RestoreNoteAsync(
        Guid id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogOperationStart("RestoreNote", entityId: id.ToString(), userId: userId);

        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId && n.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (note == null)
        {
            _logger.LogEntityNotFound<Note>(id.ToString());
            return null;
        }

        note.IsDeleted = false;
        note.DeletedAt = null;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Restored Note {NoteId} for user {UserId}", id, userId);

        return note.ToDto();
    }
}
