using Cadence.Core.Features.SystemSettings.Models.DTOs;
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
