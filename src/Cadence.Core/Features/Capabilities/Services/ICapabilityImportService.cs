using Cadence.Core.Features.Capabilities.Models.DTOs;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Service for importing predefined capability libraries into an organization.
/// </summary>
public interface ICapabilityImportService
{
    /// <summary>
    /// Imports all capabilities from a predefined library into an organization.
    /// Skips capabilities that already exist (by name, case-insensitive).
    /// </summary>
    /// <param name="organizationId">The organization to import into.</param>
    /// <param name="libraryId">The library identifier (FEMA, NATO, NIST, ISO).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Import result with counts and imported capability names.</returns>
    /// <exception cref="InvalidOperationException">Thrown if library not found or organization doesn't exist.</exception>
    Task<ImportLibraryResult> ImportLibraryAsync(
        Guid organizationId,
        string libraryId,
        CancellationToken cancellationToken = default);
}
