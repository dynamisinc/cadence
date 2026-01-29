using Cadence.Core.Features.Capabilities.Models.DTOs;
using Cadence.Core.Features.Capabilities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for organizational capability management.
/// Capabilities define the skills and functions that can be evaluated during exercises.
/// Supports multiple frameworks: FEMA Core Capabilities, NATO, NIST CSF, ISO 22301, and custom.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/capabilities")]
[Authorize]
public class CapabilitiesController : ControllerBase
{
    private readonly ICapabilityService _capabilityService;
    private readonly IPredefinedLibraryProvider _libraryProvider;
    private readonly ICapabilityImportService _importService;
    private readonly ILogger<CapabilitiesController> _logger;

    public CapabilitiesController(
        ICapabilityService capabilityService,
        IPredefinedLibraryProvider libraryProvider,
        ICapabilityImportService importService,
        ILogger<CapabilitiesController> logger)
    {
        _capabilityService = capabilityService;
        _libraryProvider = libraryProvider;
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Get all capabilities for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="includeInactive">Whether to include inactive capabilities. Default false.</param>
    /// <returns>List of capabilities ordered by category and sort order.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CapabilityDto>>> GetCapabilities(
        Guid organizationId,
        [FromQuery] bool includeInactive = false)
    {
        var capabilities = await _capabilityService.GetCapabilitiesAsync(organizationId, includeInactive);
        return Ok(capabilities);
    }

    /// <summary>
    /// Get a single capability by ID.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>The capability, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CapabilityDto>> GetCapability(Guid organizationId, Guid id)
    {
        var capability = await _capabilityService.GetCapabilityAsync(organizationId, id);

        if (capability == null)
        {
            return NotFound(new { message = "Capability not found" });
        }

        return Ok(capability);
    }

    /// <summary>
    /// Create a new capability for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="request">The create request.</param>
    /// <returns>The created capability.</returns>
    [HttpPost]
    public async Task<ActionResult<CapabilityDto>> CreateCapability(
        Guid organizationId,
        CreateCapabilityRequest request)
    {
        // Validation
        var validationError = ValidateCapabilityRequest(request.Name, request.Description, request.Category);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            var capability = await _capabilityService.CreateCapabilityAsync(organizationId, request);

            _logger.LogInformation(
                "Created capability {CapabilityId}: {CapabilityName} for organization {OrganizationId}",
                capability.Id, capability.Name, organizationId);

            return CreatedAtAction(
                nameof(GetCapability),
                new { organizationId, id = capability.Id },
                capability
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing capability.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated capability, or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CapabilityDto>> UpdateCapability(
        Guid organizationId,
        Guid id,
        UpdateCapabilityRequest request)
    {
        // Validation
        var validationError = ValidateCapabilityRequest(request.Name, request.Description, request.Category);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        try
        {
            var capability = await _capabilityService.UpdateCapabilityAsync(organizationId, id, request);

            if (capability == null)
            {
                return NotFound(new { message = "Capability not found" });
            }

            _logger.LogInformation(
                "Updated capability {CapabilityId}: {CapabilityName}",
                id, capability.Name);

            return Ok(capability);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft-delete a capability by setting it to inactive.
    /// The capability is preserved for historical data but hidden from selection UIs.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCapability(Guid organizationId, Guid id)
    {
        var deactivated = await _capabilityService.DeactivateCapabilityAsync(organizationId, id);

        if (!deactivated)
        {
            return NotFound(new { message = "Capability not found" });
        }

        _logger.LogInformation("Deactivated capability {CapabilityId}", id);

        return NoContent();
    }

    /// <summary>
    /// Reactivate a previously deactivated capability.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateCapability(Guid organizationId, Guid id)
    {
        var reactivated = await _capabilityService.ReactivateCapabilityAsync(organizationId, id);

        if (!reactivated)
        {
            return NotFound(new { message = "Capability not found" });
        }

        _logger.LogInformation("Reactivated capability {CapabilityId}", id);

        return NoContent();
    }

    /// <summary>
    /// Check if a capability name is available within an organization.
    /// Used for real-time validation in the UI.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="name">The capability name to check.</param>
    /// <param name="excludeId">Optional capability ID to exclude (for updates).</param>
    /// <returns>Object indicating whether the name is available.</returns>
    [HttpGet("check-name")]
    public async Task<ActionResult<object>> CheckCapabilityName(
        Guid organizationId,
        [FromQuery] string name,
        [FromQuery] Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "Name is required" });
        }

        var isAvailable = await _capabilityService.IsNameUniqueAsync(organizationId, name, excludeId);
        return Ok(new { isAvailable });
    }

    // =========================================================================
    // Predefined Library Import Endpoints
    // =========================================================================

    /// <summary>
    /// Get available predefined capability libraries.
    /// </summary>
    /// <param name="organizationId">The organization ID (required by route but not used).</param>
    /// <returns>List of available libraries with metadata.</returns>
    [HttpGet("libraries")]
    public ActionResult<IEnumerable<PredefinedLibraryInfo>> GetAvailableLibraries(Guid organizationId)
    {
        var libraries = _libraryProvider.GetAvailableLibraries();
        return Ok(libraries);
    }

    /// <summary>
    /// Import a predefined capability library into an organization.
    /// Skips capabilities that already exist by name (case-insensitive).
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="request">Import request containing library name.</param>
    /// <returns>Import result with counts and imported capability names.</returns>
    [HttpPost("import")]
    public async Task<ActionResult<ImportLibraryResult>> ImportLibrary(
        Guid organizationId,
        [FromBody] ImportLibraryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LibraryName))
        {
            return BadRequest(new { message = "Library name is required" });
        }

        try
        {
            var result = await _importService.ImportLibraryAsync(organizationId, request.LibraryName);

            _logger.LogInformation(
                "Imported {ImportedCount} capabilities from library '{LibraryName}' into organization {OrganizationId}. " +
                "Skipped {SkippedCount} duplicates.",
                result.Imported, request.LibraryName, organizationId, result.SkippedDuplicates);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================================
    // Private Validation Helpers
    // =========================================================================

    private static string? ValidateCapabilityRequest(string name, string? description, string? category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required";
        }

        if (name.Length < 2)
        {
            return "Name must be at least 2 characters";
        }

        if (name.Length > 200)
        {
            return "Name must be 200 characters or less";
        }

        if (description?.Length > 1000)
        {
            return "Description must be 1000 characters or less";
        }

        if (category?.Length > 100)
        {
            return "Category must be 100 characters or less";
        }

        return null;
    }
}
