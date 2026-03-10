using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Email.Services;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for user email notification preferences.
/// Users can view and toggle which email categories they receive.
/// </summary>
[ApiController]
[Route("api/users/me/email-preferences")]
[Authorize]
public class EmailPreferencesController : ControllerBase
{
    private readonly IEmailPreferenceService _preferenceService;
    private readonly ILogger<EmailPreferencesController> _logger;

    /// <summary>
    /// Display metadata for each email category.
    /// </summary>
    private static readonly Dictionary<EmailCategory, (string DisplayName, string Description)> CategoryMetadata = new()
    {
        [EmailCategory.Security] = ("Security", "Password reset and login alerts"),
        [EmailCategory.Invitations] = ("Invitations", "Organization and exercise invitations"),
        [EmailCategory.Assignments] = ("Assignments", "Inject assigned, role changed"),
        [EmailCategory.Workflow] = ("Workflow", "Inject approved or rejected"),
        [EmailCategory.Reminders] = ("Reminders", "Exercise starting, deadlines"),
        [EmailCategory.DailyDigest] = ("Daily Digest", "Daily activity summary"),
        [EmailCategory.WeeklyDigest] = ("Weekly Digest", "Weekly organization report"),
    };

    public EmailPreferencesController(
        IEmailPreferenceService preferenceService,
        ILogger<EmailPreferencesController> logger)
    {
        _preferenceService = preferenceService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's email notification preferences.
    /// Returns all 7 categories with enabled state and mandatory flag.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(EmailPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();

        var prefs = await _preferenceService.GetPreferencesAsync(userId);

        var dtos = prefs.Select(kvp =>
        {
            var meta = CategoryMetadata.GetValueOrDefault(kvp.Key, (kvp.Key.ToString(), ""));
            return new EmailPreferenceDto(
                Category: kvp.Key.ToString(),
                DisplayName: meta.Item1,
                Description: meta.Item2,
                IsEnabled: kvp.Value,
                IsMandatory: EmailPreferenceService.IsMandatoryCategory(kvp.Key)
            );
        }).OrderBy(p => (int)Enum.Parse<EmailCategory>(p.Category)).ToList();

        return Ok(new EmailPreferencesResponse(dtos));
    }

    /// <summary>
    /// Update a single email preference category for the current user.
    /// Cannot disable mandatory categories (Security, Invitations).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(EmailPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreference([FromBody] UpdateEmailPreferenceRequest request)
    {
        var userId = User.TryGetUserId();
        if (userId == null) return Unauthorized();

        if (!Enum.TryParse<EmailCategory>(request.Category, ignoreCase: true, out var category))
        {
            return BadRequest(new { message = $"Invalid email category: '{request.Category}'" });
        }

        try
        {
            await _preferenceService.UpdatePreferenceAsync(userId, category, request.IsEnabled);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        // Return the full updated preferences
        return await GetPreferences();
    }

}
