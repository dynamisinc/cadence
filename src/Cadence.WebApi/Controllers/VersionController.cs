using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// Provides version and build information for the API.
/// Used by frontend for version display and compatibility checking.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class VersionController : ControllerBase
{
    /// <summary>
    /// Returns current API version, build date, and environment information.
    /// </summary>
    /// <returns>Version information object</returns>
    [HttpGet]
    [ProducesResponseType(typeof(VersionInfo), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        // Extract version without commit hash if present (e.g., "1.0.0+abc123" -> "1.0.0")
        var cleanVersion = version.Split('+')[0];
        var commitSha = version.Contains('+') ? version.Split('+')[1] : null;

        return Ok(new VersionInfo
        {
            Version = cleanVersion,
            CommitSha = commitSha != null ? commitSha[..Math.Min(7, commitSha.Length)] : null,
            BuildDate = GetBuildDate(assembly),
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        // Try to get build date from linker timestamp
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
        {
            return System.IO.File.GetLastWriteTimeUtc(location);
        }

        return null;
    }
}

/// <summary>
/// Version information response model.
/// </summary>
public record VersionInfo
{
    /// <summary>Semantic version string (e.g., "1.2.0")</summary>
    public required string Version { get; init; }

    /// <summary>Abbreviated git commit SHA (7 characters)</summary>
    public string? CommitSha { get; init; }

    /// <summary>Build timestamp in UTC</summary>
    public DateTime? BuildDate { get; init; }

    /// <summary>Deployment environment (Development, UAT, Production)</summary>
    public required string Environment { get; init; }
}
