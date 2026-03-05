using Cadence.Core.Features.SystemSettings.Models.DTOs;

namespace Cadence.Core.Features.SystemSettings.Services;

public interface IEulaService
{
    /// <summary>
    /// Check whether the user needs to accept the current EULA.
    /// </summary>
    Task<EulaStatusDto> GetStatusAsync(string userId);

    /// <summary>
    /// Record that the user accepted a specific EULA version.
    /// </summary>
    Task AcceptAsync(string userId, string version);
}
