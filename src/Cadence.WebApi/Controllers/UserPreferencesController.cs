using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Features.Users.Services;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for user preferences.
/// Users can read and update their own display and behavior preferences.
/// </summary>
[ApiController]
[Route("api/users/me/preferences")]
[Authorize]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _preferencesService;
    private readonly ILogger<UserPreferencesController> _logger;

    public UserPreferencesController(
        IUserPreferencesService preferencesService,
        ILogger<UserPreferencesController> logger)
    {
        _preferencesService = preferencesService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's preferences.
    /// Creates default preferences if none exist.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var preferences = await _preferencesService.GetPreferencesAsync(userId);
        return Ok(preferences);
    }

    /// <summary>
    /// Update current user's preferences.
    /// Only provided fields will be updated.
    /// </summary>
    /// <param name="request">Update request with new preference values.</param>
    [HttpPut]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateUserPreferencesRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Validate enum values if provided
        if (request.Theme != null && !IsValidTheme(request.Theme))
        {
            return BadRequest(new { message = "Invalid theme value. Must be Light, Dark, or System." });
        }

        if (request.DisplayDensity != null && !IsValidDensity(request.DisplayDensity))
        {
            return BadRequest(new { message = "Invalid density value. Must be Comfortable or Compact." });
        }

        if (request.TimeFormat != null && !IsValidTimeFormat(request.TimeFormat))
        {
            return BadRequest(new { message = "Invalid time format. Must be TwentyFourHour or TwelveHour." });
        }

        var preferences = await _preferencesService.UpdatePreferencesAsync(userId, request);
        return Ok(preferences);
    }

    /// <summary>
    /// Reset current user's preferences to defaults.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(UserPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPreferences()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var preferences = await _preferencesService.ResetPreferencesAsync(userId);
        return Ok(preferences);
    }

    private string? GetCurrentUserId() => User.TryGetUserId();

    private static bool IsValidTheme(string value)
    {
        return value.Equals("Light", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Dark", StringComparison.OrdinalIgnoreCase)
            || value.Equals("System", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidDensity(string value)
    {
        return value.Equals("Comfortable", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Compact", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidTimeFormat(string value)
    {
        return value.Equals("TwentyFourHour", StringComparison.OrdinalIgnoreCase)
            || value.Equals("TwelveHour", StringComparison.OrdinalIgnoreCase);
    }
}
