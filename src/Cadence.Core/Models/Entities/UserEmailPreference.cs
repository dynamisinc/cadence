using Cadence.Core.Features.Email.Models;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// User preference for a specific email category.
/// Controls whether a user receives emails of a given type.
/// </summary>
public class UserEmailPreference : BaseEntity
{
    /// <summary>
    /// The user this preference belongs to.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The email category.
    /// </summary>
    public EmailCategory Category { get; set; }

    /// <summary>
    /// Whether emails in this category are enabled for this user.
    /// </summary>
    public bool IsEnabled { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
}
