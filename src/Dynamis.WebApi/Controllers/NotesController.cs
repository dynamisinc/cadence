using DynamisReferenceApp.Api.Tools.Notes.Models.DTOs;
using DynamisReferenceApp.Api.Tools.Notes.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DynamisReferenceApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INotesService notesService, ILogger<NotesController> logger)
    {
        _notesService = notesService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetNotes()
    {
        var userId = GetUserId(); // In real app, from User.Claims
        var notes = await _notesService.GetNotesAsync(userId);
        return Ok(notes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> GetNote(Guid id)
    {
        var userId = GetUserId();
        var note = await _notesService.GetNoteAsync(id, userId);

        if (note == null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> CreateNote(CreateNoteRequest request)
    {
        var userId = GetUserId();
        try
        {
            var note = await _notesService.CreateNoteAsync(request, userId);
            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NoteDto>> UpdateNote(Guid id, UpdateNoteRequest request)
    {
        var userId = GetUserId();
        try
        {
            var note = await _notesService.UpdateNoteAsync(id, request, userId);
            
            if (note == null)
            {
                return NotFound();
            }

            return Ok(note);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => new { Field = e.PropertyName, Message = e.ErrorMessage }));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _notesService.DeleteNoteAsync(id, userId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private string GetUserId()
    {
        // For dev/demo purposes
        return Request.Headers["X-User-Id"].FirstOrDefault() ?? "dev-user@example.com";
    }
}