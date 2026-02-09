namespace Cadence.Core.Features.SystemSettings.Models;

/// <summary>
/// Resolved email configuration with DB overrides applied on top of appsettings defaults.
/// All values are non-null and ready to use.
/// </summary>
public class ResolvedEmailConfiguration
{
    public string SupportAddress { get; set; } = string.Empty;
    public string DefaultSenderAddress { get; set; } = string.Empty;
    public string DefaultSenderName { get; set; } = string.Empty;
}
