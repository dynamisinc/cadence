using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Feedback.Services;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _context;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(AppDbContext context, ILogger<FeedbackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveAsync(
        string referenceNumber,
        FeedbackType type,
        string reporterEmail,
        string? reporterName,
        string? userRole,
        string? orgName,
        string? orgRole,
        FeedbackClientContext? clientContext,
        string title,
        string? severity,
        string? contentJson)
    {
        var report = new FeedbackReport
        {
            ReferenceNumber = referenceNumber,
            Type = type,
            Status = FeedbackStatus.New,
            Title = title,
            Severity = severity,
            ContentJson = contentJson,
            ReporterEmail = reporterEmail,
            ReporterName = reporterName,
            UserRole = userRole,
            OrgName = orgName,
            OrgRole = orgRole,
            CurrentUrl = clientContext?.CurrentUrl,
            ScreenSize = clientContext?.ScreenSize,
            AppVersion = clientContext?.AppVersion,
            CommitSha = clientContext?.CommitSha,
            ExerciseId = clientContext?.ExerciseId,
            ExerciseName = clientContext?.ExerciseName,
            ExerciseRole = clientContext?.ExerciseRole,
        };

        _context.FeedbackReports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[Feedback] Persisted {Type} report {RefNumber} from {Email}",
            type, referenceNumber, reporterEmail);
    }
}
