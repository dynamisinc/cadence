namespace Cadence.Core.Constants;

/// <summary>
/// System-wide constants for the Cadence application.
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Well-known string ID for the system ApplicationUser.
    /// Used for audit fields (CreatedBy, ModifiedBy, DeletedBy) when
    /// no authenticated user context is available (seeding, background jobs).
    /// </summary>
    public const string SystemUserIdString = "SYSTEM";

    /// <summary>
    /// Well-known ID for the default organization.
    /// </summary>
    public static readonly Guid DefaultOrganizationId = new("00000000-0000-0000-0000-000000000001");
}
