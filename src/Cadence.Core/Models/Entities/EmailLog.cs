using Cadence.Core.Features.Email.Models;

namespace Cadence.Core.Models.Entities;

/// <summary>
/// Tracks email delivery for audit and troubleshooting.
/// Organization-scoped so OrgAdmins can view logs for their organization.
/// </summary>
public class EmailLog : BaseEntity, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Recipient user ID (if the recipient is a known Cadence user).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Template used to render the email (e.g., "OrganizationInvite", "PasswordReset").
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// ACS message ID for delivery status lookups.
    /// </summary>
    public string? AcsMessageId { get; set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.Queued;

    /// <summary>
    /// Additional status details (bounce reason, error message).
    /// </summary>
    public string? StatusDetail { get; set; }

    /// <summary>
    /// When the email was sent.
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// When delivery was confirmed.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the email bounced.
    /// </summary>
    public DateTime? BouncedAt { get; set; }

    /// <summary>
    /// Type of related entity (e.g., "Exercise", "Inject", "OrganizationInvite").
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID of the related entity.
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}
