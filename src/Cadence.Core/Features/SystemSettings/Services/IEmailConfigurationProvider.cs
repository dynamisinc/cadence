using Cadence.Core.Features.SystemSettings.Models;

namespace Cadence.Core.Features.SystemSettings.Services;

/// <summary>
/// Provides resolved email configuration by merging DB overrides with appsettings defaults.
/// </summary>
public interface IEmailConfigurationProvider
{
    Task<ResolvedEmailConfiguration> GetConfigurationAsync();
}
