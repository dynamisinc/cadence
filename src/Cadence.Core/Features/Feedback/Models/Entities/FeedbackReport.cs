using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Feedback.Models.Entities;

/// <summary>
/// Persisted feedback submission. NOT org-scoped — feedback is system-wide
/// so that administrators can view all reports regardless of organization.
/// </summary>
public class FeedbackReport : BaseEntity
{
    /// <summary>Human-readable reference number (e.g. CAD-20260304-1234).</summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    public FeedbackType Type { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

    /// <summary>Title (bug/feature) or Subject (general).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Bug severity level. Null for non-bug reports.</summary>
    public string? Severity { get; set; }

    /// <summary>Full request payload stored as JSON for type-specific fields.</summary>
    public string? ContentJson { get; set; }

    // ── Reporter identity (from JWT claims) ──

    public string ReporterEmail { get; set; } = string.Empty;
    public string? ReporterName { get; set; }
    public string? UserRole { get; set; }
    public string? OrgName { get; set; }
    public string? OrgRole { get; set; }

    // ── Client context (from FeedbackClientContext) ──

    public string? CurrentUrl { get; set; }
    public string? ScreenSize { get; set; }
    public string? AppVersion { get; set; }
    public string? CommitSha { get; set; }
    public string? ExerciseId { get; set; }
    public string? ExerciseName { get; set; }
    public string? ExerciseRole { get; set; }

    // ── Admin triage ──

    /// <summary>Admin notes added during triage/review.</summary>
    public string? AdminNotes { get; set; }

    // ── GitHub integration ──

    /// <summary>GitHub issue number created for this report. Null if not configured or creation failed.</summary>
    public int? GitHubIssueNumber { get; set; }

    /// <summary>Full URL to the GitHub issue.</summary>
    public string? GitHubIssueUrl { get; set; }
}
