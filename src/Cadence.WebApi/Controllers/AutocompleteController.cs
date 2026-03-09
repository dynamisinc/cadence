using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Hubs;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for autocomplete suggestions.
/// Provides organization-scoped suggestions based on previously used values.
/// Requires authentication for all endpoints.
/// </summary>
[ApiController]
[Route("api/autocomplete")]
[Authorize]
public class AutocompleteController : ControllerBase
{
    private readonly IAutocompleteService _service;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<AutocompleteController> _logger;

    public AutocompleteController(
        IAutocompleteService service,
        ICurrentOrganizationContext orgContext,
        ILogger<AutocompleteController> logger)
    {
        _service = service;
        _orgContext = orgContext;
        _logger = logger;
    }

    /// <summary>
    /// Get track suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/tracks")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetTrackSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetTrackSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get target suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/targets")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetTargetSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetTargetSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get source suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/sources")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetSourceSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetSourceSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get location name suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/location-names")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetLocationNameSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetLocationNameSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get location type suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/location-types")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetLocationTypeSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetLocationTypeSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get responsible controller suggestions for an exercise's organization.
    /// </summary>
    [HttpGet("exercises/{exerciseId:guid}/responsible-controllers")]
    [AuthorizeExerciseAccess]
    public async Task<ActionResult<IEnumerable<string>>> GetResponsibleControllerSuggestions(
        Guid exerciseId,
        [FromQuery] string? filter = null,
        [FromQuery] int limit = 20)
    {
        var (organizationId, error) = await ValidateExerciseAccessAsync(exerciseId);
        if (error != null) return error;

        var suggestions = await _service.GetResponsibleControllerSuggestionsAsync(organizationId!.Value, filter, limit);
        return Ok(suggestions);
    }

    /// <summary>
    /// Validates the user has access to the exercise's organization.
    /// Returns the organization ID if access is granted, or an error ActionResult if denied.
    /// </summary>
    private async Task<(Guid? OrganizationId, ActionResult? Error)> ValidateExerciseAccessAsync(Guid exerciseId)
    {
        var organizationId = await _service.GetExerciseOrganizationIdAsync(exerciseId);

        if (organizationId == null)
            return (null, NotFound(new { message = "Exercise not found" }));

        // SysAdmins can access any organization
        if (_orgContext.IsSysAdmin)
            return (organizationId, null);

        // Regular users must have a current organization context matching the exercise's org
        if (!_orgContext.CurrentOrganizationId.HasValue ||
            _orgContext.CurrentOrganizationId.Value != organizationId.Value)
        {
            _logger.LogWarning(
                "User attempted to access autocomplete for exercise {ExerciseId} in organization {ExerciseOrgId} but is in organization {CurrentOrgId}",
                exerciseId, organizationId.Value, _orgContext.CurrentOrganizationId);
            return (null, Forbid());
        }

        return (organizationId, null);
    }
}
