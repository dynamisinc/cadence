using System.Security.Claims;
using Cadence.Core.Features.SystemSettings.Models.DTOs;
using Cadence.Core.Features.SystemSettings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

[ApiController]
[Route("api/eula")]
[Authorize]
public class EulaController : ControllerBase
{
    private readonly IEulaService _eulaService;

    public EulaController(IEulaService eulaService)
    {
        _eulaService = eulaService;
    }

    /// <summary>
    /// Get the current user's EULA acceptance status.
    /// Returns whether acceptance is required and the EULA content if so.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(EulaStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        var status = await _eulaService.GetStatusAsync(userId);
        return Ok(status);
    }

    /// <summary>
    /// Accept the current EULA version.
    /// </summary>
    [HttpPost("accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Accept([FromBody] AcceptEulaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Version))
            return BadRequest(new { error = "Version is required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        await _eulaService.AcceptAsync(userId, request.Version);
        return NoContent();
    }
}
