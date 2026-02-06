namespace Cadence.Core.Features.Email.Models;

/// <summary>
/// Template model for daily activity digest.
/// </summary>
public class DailyDigestEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string DigestDate { get; set; } = string.Empty;
    public string ActivitySummary { get; set; } = string.Empty;
    public string DashboardUrl { get; set; } = string.Empty;
    public string PreferencesUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for Exercise Director daily summary.
/// </summary>
public class DirectorDailySummaryEmailModel
{
    public string DirectorName { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public string SummaryDate { get; set; } = string.Empty;
    public string DaysUntilExercise { get; set; } = string.Empty;
    public string MselStatus { get; set; } = string.Empty;
    public string AttentionItems { get; set; } = string.Empty;
    public string ParticipantStatus { get; set; } = string.Empty;
    public string ExerciseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Template model for weekly organization report.
/// </summary>
public class WeeklyOrgReportEmailModel
{
    public string RecipientName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string ReportPeriod { get; set; } = string.Empty;
    public string ActivityMetrics { get; set; } = string.Empty;
    public string UpcomingExercises { get; set; } = string.Empty;
    public string TeamUpdates { get; set; } = string.Empty;
    public string DashboardUrl { get; set; } = string.Empty;
}
