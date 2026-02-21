using Cadence.Core.Features.Autocomplete.Models.DTOs;
using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for managing organization-curated autocomplete suggestions.
/// OrgAdmins can add, update, delete, and reorder suggestions per inject field.
/// </summary>
[ApiController]
[Route("api/organizations/current/suggestions")]
[Authorize]
[AuthorizeOrgAdmin]
public class OrganizationSuggestionsController : ControllerBase
{
    private readonly IOrganizationSuggestionService _service;
    private readonly IAutocompleteService _autocompleteService;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly ILogger<OrganizationSuggestionsController> _logger;

    public OrganizationSuggestionsController(
        IOrganizationSuggestionService service,
        IAutocompleteService autocompleteService,
        ICurrentOrganizationContext orgContext,
        ILogger<OrganizationSuggestionsController> logger)
    {
        _service = service;
        _autocompleteService = autocompleteService;
        _orgContext = orgContext;
        _logger = logger;
    }

    private Guid? GetCurrentOrganizationId() => _orgContext.CurrentOrganizationId;

    /// <summary>
    /// Get all suggestions for a field in the current organization.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationSuggestionDto>>> GetSuggestions(
        [FromQuery] string fieldName,
        [FromQuery] bool includeInactive = false)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        if (!SuggestionFieldNames.IsValid(fieldName))
            return BadRequest(new { message = $"Invalid field name: {fieldName}" });

        var suggestions = await _service.GetSuggestionsAsync(orgId.Value, fieldName, includeInactive);
        return Ok(suggestions);
    }

    /// <summary>
    /// Get a single suggestion by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationSuggestionDto>> GetSuggestion(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        var suggestion = await _service.GetSuggestionAsync(orgId.Value, id);
        if (suggestion == null)
            return NotFound(new { message = "Suggestion not found" });

        return Ok(suggestion);
    }

    /// <summary>
    /// Create a new managed suggestion.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationSuggestionDto>> CreateSuggestion(
        [FromBody] CreateSuggestionRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        try
        {
            var suggestion = await _service.CreateSuggestionAsync(orgId.Value, request);

            _logger.LogInformation(
                "Created suggestion '{Value}' for field {FieldName} in organization {OrgId}",
                suggestion.Value, suggestion.FieldName, orgId.Value);

            return CreatedAtAction(nameof(GetSuggestion), new { id = suggestion.Id }, suggestion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing suggestion.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationSuggestionDto>> UpdateSuggestion(
        Guid id, [FromBody] UpdateSuggestionRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        try
        {
            var suggestion = await _service.UpdateSuggestionAsync(orgId.Value, id, request);
            if (suggestion == null)
                return NotFound(new { message = "Suggestion not found" });

            return Ok(suggestion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft-delete a suggestion.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSuggestion(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        var deleted = await _service.DeleteSuggestionAsync(orgId.Value, id);
        if (!deleted)
            return NotFound(new { message = "Suggestion not found" });

        return NoContent();
    }

    /// <summary>
    /// Bulk-create suggestions from a list of values (paste support).
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateSuggestionsResult>> BulkCreateSuggestions(
        [FromBody] BulkCreateSuggestionsRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        try
        {
            var result = await _service.BulkCreateSuggestionsAsync(orgId.Value, request);

            _logger.LogInformation(
                "Bulk created {Created} suggestions for field {FieldName} in organization {OrgId} ({Skipped} duplicates skipped)",
                result.Created, request.FieldName, orgId.Value, result.SkippedDuplicates);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reorder suggestions within a field.
    /// </summary>
    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderSuggestions(
        [FromQuery] string fieldName, [FromBody] List<Guid> orderedIds)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        if (!SuggestionFieldNames.IsValid(fieldName))
            return BadRequest(new { message = $"Invalid field name: {fieldName}" });

        try
        {
            await _service.ReorderSuggestionsAsync(orgId.Value, fieldName, orderedIds);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get historical values for a field, excluding curated and blocked values.
    /// Used by the management page to show blockable historical suggestions.
    /// </summary>
    [HttpGet("historical")]
    public async Task<ActionResult<IEnumerable<string>>> GetHistoricalValues(
        [FromQuery] string fieldName, [FromQuery] int limit = 50)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        if (!SuggestionFieldNames.IsValid(fieldName))
            return BadRequest(new { message = $"Invalid field name: {fieldName}" });

        var values = await _autocompleteService.GetHistoricalValuesAsync(orgId.Value, fieldName, limit);
        return Ok(values);
    }

    /// <summary>
    /// Block a historical value from appearing in autocomplete suggestions.
    /// </summary>
    [HttpPost("block")]
    public async Task<ActionResult<OrganizationSuggestionDto>> BlockValue(
        [FromBody] BlockSuggestionRequest request)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        try
        {
            var blocked = await _service.BlockValueAsync(orgId.Value, request);

            _logger.LogInformation(
                "Blocked value '{Value}' for field {FieldName} in organization {OrgId}",
                request.Value, request.FieldName, orgId.Value);

            return Ok(blocked);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Unblock a previously blocked value.
    /// </summary>
    [HttpDelete("block/{id:guid}")]
    public async Task<IActionResult> UnblockValue(Guid id)
    {
        var orgId = GetCurrentOrganizationId();
        if (orgId == null)
            return NotFound(new { message = "No organization context" });

        var unblocked = await _service.UnblockAsync(orgId.Value, id);
        if (!unblocked)
            return NotFound(new { message = "Blocked value not found" });

        return NoContent();
    }
}
