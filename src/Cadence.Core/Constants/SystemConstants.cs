namespace Cadence.Core.Constants;

/// <summary>
/// System-wide constants for the Cadence application.
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Well-known ID for the system user. Used for operations
    /// performed before authentication is implemented or for
    /// automated system actions.
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Well-known ID for the default organization.
    /// </summary>
    public static readonly Guid DefaultOrganizationId = new("00000000-0000-0000-0000-000000000001");
}
