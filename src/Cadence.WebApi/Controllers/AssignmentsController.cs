using System.Security.Claims;
using Cadence.Core.Features.Assignments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// Controller for managing user exercise assignments.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(
        IAssignmentService assignmentService,
        ILogger<AssignmentsController> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all exercise assignments for the current user, grouped by status.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignments grouped into Active, Upcoming, and Completed sections</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyAssignments(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetMyAssignments called without authenticated user");
            return Unauthorized();
        }

        var assignments = await _assignmentService.GetMyAssignmentsAsync(userId, ct);
        return Ok(assignments);
    }

    /// <summary>
    /// Get assignment details for a specific exercise.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignment details or 404 if not found</returns>
    [HttpGet("my/{exerciseId:guid}")]
    public async Task<IActionResult> GetMyAssignment(Guid exerciseId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GetMyAssignment called without authenticated user");
            return Unauthorized();
        }

        var assignment = await _assignmentService.GetAssignmentAsync(userId, exerciseId, ct);
        if (assignment == null)
        {
            return NotFound();
        }

        return Ok(assignment);
    }
}
