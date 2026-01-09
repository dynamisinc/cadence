using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Cadence.Api.Tools.Notes.Models.DTOs;
using Cadence.Api.Tools.Notes.Services;
using Cadence.Api.Hubs;

namespace Cadence.Api.Tools.Notes.Functions;

/// <summary>
/// Output binding class for functions that return both HTTP response and SignalR message.
/// </summary>
public class NoteWithSignalROutput
{
    [HttpResult]
    public required IActionResult HttpResponse { get; set; }

    // [SignalROutput(HubName = "notifications")]
    // public SignalRMessageAction? SignalRMessage { get; set; }
}

/// <summary>
/// HTTP trigger functions for Notes CRUD operations.
/// </summary>
public class NotesFunction
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesFunction> _logger;

    public NotesFunction(INotesService notesService, ILogger<NotesFunction> logger)
    {
        _notesService = notesService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all notes for the current user.
    /// </summary>
    [Function("GetNotes")]
    public async Task<IActionResult> GetNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes")] HttpRequest req,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("GetNotes called for user {UserId}", userId);

        var notes = await _notesService.GetNotesAsync(userId);

        return new OkObjectResult(notes);
    }

    /// <summary>
    /// Gets a single note by ID.
    /// </summary>
    [Function("GetNote")]
    public async Task<IActionResult> GetNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes/{id:guid}")] HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("GetNote called for note {NoteId} by user {UserId}", id, userId);

        var note = await _notesService.GetNoteAsync(id, userId);

        if (note == null)
        {
            return new NotFoundObjectResult(new { message = "Note not found" });
        }

        return new OkObjectResult(note);
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    [Function("CreateNote")]
    public async Task<NoteWithSignalROutput> CreateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notes")] HttpRequest req,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("CreateNote called by user {UserId}", userId);

        var request = await ParseRequestBody<CreateNoteRequest>(req);
        if (request == null)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new { message = "Invalid request body" })
            };
        }

        try
        {
            var note = await _notesService.CreateNoteAsync(request, userId);
            return new NoteWithSignalROutput
            {
                HttpResponse = new CreatedResult($"/api/notes/{note.Id}", note),
                // SignalRMessage = SignalRBroadcastExtensions.NoteCreated(note.Id, userId)
            };
        }
        catch (ArgumentException ex)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new { message = ex.Message })
            };
        }
    }

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    [Function("UpdateNote")]
    public async Task<NoteWithSignalROutput> UpdateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "notes/{id:guid}")] HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("UpdateNote called for note {NoteId} by user {UserId}", id, userId);

        var request = await ParseRequestBody<UpdateNoteRequest>(req);
        if (request == null)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new { message = "Invalid request body" })
            };
        }

        try
        {
            var note = await _notesService.UpdateNoteAsync(id, request, userId);

            if (note == null)
            {
                return new NoteWithSignalROutput
                {
                    HttpResponse = new NotFoundObjectResult(new { message = "Note not found" })
                };
            }

            return new NoteWithSignalROutput
            {
                HttpResponse = new OkObjectResult(note),
                // SignalRMessage = SignalRBroadcastExtensions.NoteUpdated(id, userId)
            };
        }
        catch (ArgumentException ex)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new BadRequestObjectResult(new { message = ex.Message })
            };
        }
    }

    /// <summary>
    /// Deletes a note (soft delete).
    /// </summary>
    [Function("DeleteNote")]
    public async Task<NoteWithSignalROutput> DeleteNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "notes/{id:guid}")] HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("DeleteNote called for note {NoteId} by user {UserId}", id, userId);

        var deleted = await _notesService.DeleteNoteAsync(id, userId);

        if (!deleted)
        {
            return new NoteWithSignalROutput
            {
                HttpResponse = new NotFoundObjectResult(new { message = "Note not found" })
            };
        }

        return new NoteWithSignalROutput
        {
            HttpResponse = new NoContentResult(),
            // SignalRMessage = SignalRBroadcastExtensions.NoteDeleted(id, userId)
        };
    }

    /// <summary>
    /// Restores a soft-deleted note.
    /// </summary>
    [Function("RestoreNote")]
    public async Task<IActionResult> RestoreNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notes/{id:guid}/restore")] HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("RestoreNote called for note {NoteId} by user {UserId}", id, userId);

        var note = await _notesService.RestoreNoteAsync(id, userId);

        if (note == null)
        {
            return new NotFoundObjectResult(new { message = "Note not found or not deleted" });
        }

        return new OkObjectResult(note);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Gets the user ID from the request.
    /// In production, this would come from authentication.
    /// For development, it falls back to a header or default value.
    /// </summary>
    private static string GetUserId(HttpRequest req)
    {
        // Check for user ID in headers (for development/testing)
        if (req.Headers.TryGetValue("X-User-Id", out var userId) && !string.IsNullOrEmpty(userId))
        {
            return userId!;
        }

        // In production, this would come from authentication claims
        // For now, return a default development user
        return "dev-user@example.com";
    }

    /// <summary>
    /// Parses the request body as JSON.
    /// </summary>
    private static async Task<T?> ParseRequestBody<T>(HttpRequest req) where T : class
    {
        try
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
