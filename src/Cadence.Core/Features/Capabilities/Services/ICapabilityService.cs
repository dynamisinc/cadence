using Cadence.Core.Features.Capabilities.Models.DTOs;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Service interface for managing organizational capabilities.
/// </summary>
public interface ICapabilityService
{
    /// <summary>
    /// Gets all capabilities for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="includeInactive">Whether to include inactive capabilities. Default false.</param>
    /// <returns>List of capabilities ordered by category and sort order.</returns>
    Task<IEnumerable<CapabilityDto>> GetCapabilitiesAsync(Guid organizationId, bool includeInactive = false);

    /// <summary>
    /// Gets a single capability by ID.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>The capability DTO, or null if not found.</returns>
    Task<CapabilityDto?> GetCapabilityAsync(Guid organizationId, Guid id);

    /// <summary>
    /// Creates a new capability for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="request">The create request.</param>
    /// <returns>The created capability DTO.</returns>
    /// <exception cref="InvalidOperationException">Thrown if name already exists.</exception>
    Task<CapabilityDto> CreateCapabilityAsync(Guid organizationId, CreateCapabilityRequest request);

    /// <summary>
    /// Updates an existing capability.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated capability DTO, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if name already exists for another capability.</exception>
    Task<CapabilityDto?> UpdateCapabilityAsync(Guid organizationId, Guid id, UpdateCapabilityRequest request);

    /// <summary>
    /// Soft-deletes a capability by setting IsActive to false.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>True if deactivated, false if not found.</returns>
    Task<bool> DeactivateCapabilityAsync(Guid organizationId, Guid id);

    /// <summary>
    /// Reactivates a previously deactivated capability.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="id">The capability ID.</param>
    /// <returns>True if reactivated, false if not found.</returns>
    Task<bool> ReactivateCapabilityAsync(Guid organizationId, Guid id);

    /// <summary>
    /// Checks if a capability name is unique within an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="name">The capability name to check.</param>
    /// <param name="excludeId">Optional capability ID to exclude (for updates).</param>
    /// <returns>True if the name is unique, false if it already exists.</returns>
    Task<bool> IsNameUniqueAsync(Guid organizationId, string name, Guid? excludeId = null);
}
