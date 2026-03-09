using Cadence.Core.Features.Feedback.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Entities;

namespace Cadence.Core.Features.Feedback.Mappers;

/// <summary>
/// Extension methods for mapping between FeedbackReport entity and DTOs.
/// </summary>
public static class FeedbackMapper
{
    /// <summary>
    /// Maps a FeedbackReport entity to a FeedbackReportDto.
    /// </summary>
    public static FeedbackReportDto ToDto(this FeedbackReport entity) => new()
    {
        Id = entity.Id,
        ReferenceNumber = entity.ReferenceNumber,
        Type = entity.Type,
        Status = entity.Status,
        Title = entity.Title,
        Severity = entity.Severity,
        ContentJson = entity.ContentJson,
        ReporterEmail = entity.ReporterEmail,
        ReporterName = entity.ReporterName,
        UserRole = entity.UserRole,
        OrgName = entity.OrgName,
        OrgRole = entity.OrgRole,
        CurrentUrl = entity.CurrentUrl,
        ScreenSize = entity.ScreenSize,
        AppVersion = entity.AppVersion,
        CommitSha = entity.CommitSha,
        ExerciseId = entity.ExerciseId,
        ExerciseName = entity.ExerciseName,
        ExerciseRole = entity.ExerciseRole,
        AdminNotes = entity.AdminNotes,
        GitHubIssueNumber = entity.GitHubIssueNumber,
        GitHubIssueUrl = entity.GitHubIssueUrl,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
