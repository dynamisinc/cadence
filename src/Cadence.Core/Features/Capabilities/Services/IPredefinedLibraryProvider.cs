using Cadence.Core.Features.Capabilities.Models.DTOs;

namespace Cadence.Core.Features.Capabilities.Services;

/// <summary>
/// Provides access to predefined capability libraries (FEMA, NATO, NIST, ISO).
/// </summary>
public interface IPredefinedLibraryProvider
{
    /// <summary>
    /// Gets metadata for all available predefined libraries.
    /// </summary>
    /// <returns>Collection of library information records.</returns>
    IEnumerable<PredefinedLibraryInfo> GetAvailableLibraries();

    /// <summary>
    /// Gets the full capability list for a specific library.
    /// </summary>
    /// <param name="libraryId">The library identifier (FEMA, NATO, NIST, ISO).</param>
    /// <returns>Library definition with capabilities, or null if not found.</returns>
    PredefinedLibrary? GetLibrary(string libraryId);
}

/// <summary>
/// Represents a predefined capability library with its metadata and capabilities.
/// </summary>
public record PredefinedLibrary(
    string Id,
    string Name,
    string Description,
    List<PredefinedCapability> Capabilities
);

/// <summary>
/// Represents a capability from a predefined library.
/// </summary>
public record PredefinedCapability(
    string Name,
    string? Description,
    string? Category
);
