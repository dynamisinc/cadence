using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Features.Users.Models.DTOs;

namespace Cadence.Core.Features.Feedback.Models.DTOs;

public record FeedbackReportDto
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public FeedbackType Type { get; init; }
    public FeedbackStatus Status { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Severity { get; init; }
    public string? ContentJson { get; init; }
    public string ReporterEmail { get; init; } = string.Empty;
    public string? ReporterName { get; init; }
    public string? UserRole { get; init; }
    public string? OrgName { get; init; }
    public string? OrgRole { get; init; }
    public string? CurrentUrl { get; init; }
    public string? ScreenSize { get; init; }
    public string? AppVersion { get; init; }
    public string? CommitSha { get; init; }
    public string? ExerciseId { get; init; }
    public string? ExerciseName { get; init; }
    public string? ExerciseRole { get; init; }
    public string? AdminNotes { get; init; }
    public int? GitHubIssueNumber { get; init; }
    public string? GitHubIssueUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record UpdateFeedbackStatusRequest
{
    public FeedbackStatus Status { get; init; }
    public string? AdminNotes { get; init; }
}

public record FeedbackListResponse
{
    public List<FeedbackReportDto> Reports { get; init; } = new();
    public PaginationInfo Pagination { get; init; } = new();
}
