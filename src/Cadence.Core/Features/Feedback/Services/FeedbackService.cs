using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Features.Users.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Feedback.Services;

public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _context;
    private readonly IGitHubIssueService _gitHubIssueService;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(
        AppDbContext context,
        IGitHubIssueService gitHubIssueService,
        ILogger<FeedbackService> logger)
    {
        _context = context;
        _gitHubIssueService = gitHubIssueService;
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

        // Best-effort GitHub issue creation
        try
        {
            var result = await _gitHubIssueService.CreateIssueAsync(
                referenceNumber, type, title, severity, contentJson,
                reporterEmail, reporterName, orgName);

            if (result.HasValue)
            {
                report.GitHubIssueNumber = result.Value.IssueNumber;
                report.GitHubIssueUrl = result.Value.IssueUrl;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Feedback] GitHub issue creation failed for {RefNumber} (non-critical)",
                referenceNumber);
        }
    }

    public async Task<(FeedbackStatus Status, string? AdminNotes)> UpdateStatusAsync(Guid id, FeedbackStatus status, string? adminNotes)
    {
        var report = await _context.FeedbackReports.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Feedback report {id} not found");

        var previousStatus = report.Status;
        var previousNotes = report.AdminNotes;

        report.Status = status;
        report.AdminNotes = adminNotes;

        var rowsAffected = await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[Feedback] Updated {RefNumber} status {PreviousStatus} → {Status} ({RowsAffected} rows affected)",
            report.ReferenceNumber, previousStatus, status, rowsAffected);

        // Best-effort GitHub sync
        if (report.GitHubIssueNumber.HasValue)
        {
            try
            {
                // If admin notes changed, add as comment
                if (!string.IsNullOrEmpty(adminNotes) && adminNotes != previousNotes)
                {
                    await _gitHubIssueService.AddIssueCommentAsync(
                        report.GitHubIssueNumber.Value,
                        $"**Admin Note** ({report.ReferenceNumber}):\n\n{adminNotes}");
                }

                // If status set to Closed, close the GH issue
                if (status == FeedbackStatus.Closed)
                {
                    await _gitHubIssueService.CloseIssueAsync(
                        report.GitHubIssueNumber.Value,
                        $"Closed in Cadence ({report.ReferenceNumber}).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[Feedback] GitHub sync failed for {RefNumber} (non-critical)",
                    report.ReferenceNumber);
            }
        }

        return (report.Status, report.AdminNotes);
    }

    public async Task SoftDeleteAsync(Guid id, string deletedBy)
    {
        var report = await _context.FeedbackReports.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Feedback report {id} not found");

        report.IsDeleted = true;
        report.DeletedAt = DateTime.UtcNow;
        report.DeletedBy = deletedBy;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[Feedback] Soft-deleted {RefNumber} by {DeletedBy}",
            report.ReferenceNumber, deletedBy);

        // Best-effort: close the GitHub issue
        if (report.GitHubIssueNumber.HasValue)
        {
            try
            {
                await _gitHubIssueService.CloseIssueAsync(
                    report.GitHubIssueNumber.Value,
                    $"Deleted in Cadence ({report.ReferenceNumber}).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[Feedback] GitHub sync failed for {RefNumber} (non-critical)",
                    report.ReferenceNumber);
            }
        }
    }

    public async Task<FeedbackListResponse> GetReportsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        FeedbackType? type = null,
        FeedbackStatus? status = null,
        string? sortBy = null,
        bool sortDesc = true)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.FeedbackReports.AsQueryable();

        if (type.HasValue)
            query = query.Where(r => r.Type == type.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Title.Contains(term) ||
                r.ReferenceNumber.Contains(term) ||
                r.ReporterEmail.Contains(term) ||
                (r.ReporterName != null && r.ReporterName.Contains(term)));
        }

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLowerInvariant() switch
        {
            "title" => sortDesc ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
            "referencenumber" => sortDesc ? query.OrderByDescending(r => r.ReferenceNumber) : query.OrderBy(r => r.ReferenceNumber),
            "type" => sortDesc ? query.OrderByDescending(r => r.Type) : query.OrderBy(r => r.Type),
            "status" => sortDesc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "reporteremail" => sortDesc ? query.OrderByDescending(r => r.ReporterEmail) : query.OrderBy(r => r.ReporterEmail),
            _ => sortDesc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
        };

        var reports = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new FeedbackReportDto
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                Type = r.Type,
                Status = r.Status,
                Title = r.Title,
                Severity = r.Severity,
                ContentJson = r.ContentJson,
                ReporterEmail = r.ReporterEmail,
                ReporterName = r.ReporterName,
                UserRole = r.UserRole,
                OrgName = r.OrgName,
                OrgRole = r.OrgRole,
                CurrentUrl = r.CurrentUrl,
                ScreenSize = r.ScreenSize,
                AppVersion = r.AppVersion,
                CommitSha = r.CommitSha,
                ExerciseId = r.ExerciseId,
                ExerciseName = r.ExerciseName,
                ExerciseRole = r.ExerciseRole,
                AdminNotes = r.AdminNotes,
                GitHubIssueNumber = r.GitHubIssueNumber,
                GitHubIssueUrl = r.GitHubIssueUrl,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToListAsync();

        return new FeedbackListResponse
        {
            Reports = reports,
            Pagination = new PaginationInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            },
        };
    }
}
