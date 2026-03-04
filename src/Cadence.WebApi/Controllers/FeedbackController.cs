using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Features.Feedback.Services;
using Cadence.Core.Features.SystemSettings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for user feedback: bug reports, feature requests, general feedback, and error reports.
/// Sends templated emails to the support team and acknowledgments to the user.
/// </summary>
[ApiController]
[Route("api/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IEmailConfigurationProvider _emailConfig;
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IEmailService emailService,
        IEmailConfigurationProvider emailConfig,
        IFeedbackService feedbackService,
        ILogger<FeedbackController> logger)
    {
        _emailService = emailService;
        _emailConfig = emailConfig;
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a bug report.
    /// </summary>
    [HttpPost("bug-report")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitBugReport([FromBody] SubmitBugReportRequest request)
    {
        var userCtx = GetCurrentUserContext();
        if (userCtx.Email == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Feedback] Bug report received - Ref: {RefNumber}, User: {Email}, Title: {Title}, Severity: {Severity}",
            refNumber, userCtx.Email, request.Title, request.Severity);

        try
        {
            var model = new BugReportEmailModel
            {
                Title = request.Title,
                Description = request.Description,
                StepsToReproduce = request.StepsToReproduce ?? "Not provided",
                Severity = request.Severity,
                ReporterName = userCtx.Name ?? "Unknown",
                ReporterEmail = userCtx.Email,
                // Client-supplied environment context (more reliable than HTTP headers for a SPA)
                CurrentUrl = request.Context?.CurrentUrl ?? Request.Headers.Referer.ToString(),
                Browser = Request.Headers.UserAgent.ToString(),
                ScreenSize = request.Context?.ScreenSize ?? "Unknown",
                OperatingSystem = "Detected from user agent",
                ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                // Identity and org context from JWT claims
                UserRole = userCtx.SystemRole ?? "Unknown",
                OrgName = userCtx.OrgName ?? "No organisation",
                OrgRole = userCtx.OrgRole ?? "Unknown",
                // Exercise context from client
                ExerciseName = request.Context?.ExerciseName ?? "Not in exercise",
                ExerciseRole = FormatExerciseRole(request.Context?.ExerciseRole),
                // App version from client
                AppVersion = request.Context?.AppVersion ?? "Unknown",
                CommitSha = request.Context?.CommitSha ?? "Unknown",
            };

            var config = await _emailConfig.GetConfigurationAsync();
            var supportRecipient = new EmailRecipient(config.SupportAddress, "Cadence Support");
            var supportResult = await _emailService.SendTemplatedAsync("BugReport", model, supportRecipient);

            if (supportResult.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "[Feedback] Support email failed for bug report {RefNumber}: {Error}",
                    refNumber, supportResult.ErrorMessage);
            }

            await SendAcknowledgmentSafe(userCtx.Name ?? "User", userCtx.Email, refNumber, "Bug Report", request.Title);

            await PersistFeedbackSafe(refNumber, FeedbackType.BugReport, userCtx, request.Context, request.Title, request.Severity,
                JsonSerializer.Serialize(new { request.Description, request.StepsToReproduce, request.Severity }));

            _logger.LogInformation(
                "[Feedback] Bug report complete - Ref: {RefNumber}, User: {Email}, " +
                "SupportEmailStatus: {SupportStatus}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, supportResult.Status, sw.ElapsedMilliseconds);

            return Ok(new FeedbackResponse(refNumber, "Bug report submitted successfully. You'll receive a confirmation email shortly."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Feedback] Bug report FAILED - Ref: {RefNumber}, User: {Email}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, sw.ElapsedMilliseconds);
            return StatusCode(500, new { message = "Failed to submit bug report. Please try again." });
        }
    }

    /// <summary>
    /// Submit a feature request.
    /// </summary>
    [HttpPost("feature-request")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeatureRequest([FromBody] SubmitFeatureRequestRequest request)
    {
        var userCtx = GetCurrentUserContext();
        if (userCtx.Email == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Feedback] Feature request received - Ref: {RefNumber}, User: {Email}, Title: {Title}",
            refNumber, userCtx.Email, request.Title);

        try
        {
            var model = new FeatureRequestEmailModel
            {
                Title = request.Title,
                Description = request.Description,
                UseCase = request.UseCase ?? "Not provided",
                ReporterName = userCtx.Name ?? "Unknown",
                ReporterEmail = userCtx.Email,
                ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                // Identity and org context from JWT claims
                UserRole = userCtx.SystemRole ?? "Unknown",
                OrgName = userCtx.OrgName ?? "No organisation",
                OrgRole = userCtx.OrgRole ?? "Unknown",
                // Exercise context from client
                ExerciseName = request.Context?.ExerciseName ?? "Not in exercise",
                ExerciseRole = FormatExerciseRole(request.Context?.ExerciseRole),
                // App version from client
                AppVersion = request.Context?.AppVersion ?? "Unknown",
                CommitSha = request.Context?.CommitSha ?? "Unknown",
            };

            var config = await _emailConfig.GetConfigurationAsync();
            var supportRecipient = new EmailRecipient(config.SupportAddress, "Cadence Support");
            var supportResult = await _emailService.SendTemplatedAsync("FeatureRequest", model, supportRecipient);

            if (supportResult.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "[Feedback] Support email failed for feature request {RefNumber}: {Error}",
                    refNumber, supportResult.ErrorMessage);
            }

            await SendAcknowledgmentSafe(userCtx.Name ?? "User", userCtx.Email, refNumber, "Feature Request", request.Title);

            await PersistFeedbackSafe(refNumber, FeedbackType.FeatureRequest, userCtx, request.Context, request.Title, null,
                JsonSerializer.Serialize(new { request.Description, request.UseCase }));

            _logger.LogInformation(
                "[Feedback] Feature request complete - Ref: {RefNumber}, User: {Email}, " +
                "SupportEmailStatus: {SupportStatus}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, supportResult.Status, sw.ElapsedMilliseconds);

            return Ok(new FeedbackResponse(refNumber, "Feature request submitted successfully. Thank you for your feedback!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Feedback] Feature request FAILED - Ref: {RefNumber}, User: {Email}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, sw.ElapsedMilliseconds);
            return StatusCode(500, new { message = "Failed to submit feature request. Please try again." });
        }
    }

    /// <summary>
    /// Submit general feedback.
    /// </summary>
    [HttpPost("general")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitGeneralFeedback([FromBody] SubmitGeneralFeedbackRequest request)
    {
        var userCtx = GetCurrentUserContext();
        if (userCtx.Email == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Feedback] General feedback received - Ref: {RefNumber}, User: {Email}, Category: {Category}, Subject: {Subject}",
            refNumber, userCtx.Email, request.Category, request.Subject);

        try
        {
            var model = new GeneralFeedbackEmailModel
            {
                Category = request.Category,
                Subject = request.Subject,
                Message = request.Message,
                SenderName = userCtx.Name ?? "Unknown",
                SenderEmail = userCtx.Email,
                SentAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                // Identity and org context from JWT claims
                UserRole = userCtx.SystemRole ?? "Unknown",
                OrgName = userCtx.OrgName ?? "No organisation",
                OrgRole = userCtx.OrgRole ?? "Unknown",
                // Exercise context from client
                ExerciseName = request.Context?.ExerciseName ?? "Not in exercise",
                ExerciseRole = FormatExerciseRole(request.Context?.ExerciseRole),
                // App version from client
                AppVersion = request.Context?.AppVersion ?? "Unknown",
                CommitSha = request.Context?.CommitSha ?? "Unknown",
            };

            var config = await _emailConfig.GetConfigurationAsync();
            var supportRecipient = new EmailRecipient(config.SupportAddress, "Cadence Support");
            var supportResult = await _emailService.SendTemplatedAsync("GeneralFeedback", model, supportRecipient);

            if (supportResult.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "[Feedback] Support email failed for general feedback {RefNumber}: {Error}",
                    refNumber, supportResult.ErrorMessage);
            }

            await SendAcknowledgmentSafe(userCtx.Name ?? "User", userCtx.Email, refNumber, "Feedback", request.Subject);

            await PersistFeedbackSafe(refNumber, FeedbackType.General, userCtx, request.Context, request.Subject, null,
                JsonSerializer.Serialize(new { request.Category, request.Message }));

            _logger.LogInformation(
                "[Feedback] General feedback complete - Ref: {RefNumber}, User: {Email}, " +
                "SupportEmailStatus: {SupportStatus}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, supportResult.Status, sw.ElapsedMilliseconds);

            return Ok(new FeedbackResponse(refNumber, "Feedback submitted successfully. Thank you!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Feedback] General feedback FAILED - Ref: {RefNumber}, User: {Email}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, sw.ElapsedMilliseconds);
            return StatusCode(500, new { message = "Failed to submit feedback. Please try again." });
        }
    }

    /// <summary>
    /// Submit an error report from the ErrorBoundary component.
    /// </summary>
    [HttpPost("error-report")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitErrorReport([FromBody] SubmitErrorReportRequest request)
    {
        var userCtx = GetCurrentUserContext();
        if (userCtx.Email == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Feedback] Error report received - Ref: {RefNumber}, User: {Email}, " +
            "ErrorMessage: {ErrorMessage}, Url: {Url}",
            refNumber, userCtx.Email, Truncate(request.ErrorMessage, 200), request.Url);

        _logger.LogDebug(
            "[Feedback] Error report details - Ref: {RefNumber}, StackTrace: {StackTrace}, " +
            "ComponentStack: {ComponentStack}, Browser: {Browser}",
            refNumber, request.StackTrace, request.ComponentStack, request.Browser);

        try
        {
            var model = new BugReportEmailModel
            {
                Title = $"[Auto] Runtime Error: {Truncate(request.ErrorMessage, 100)}",
                Description = request.ErrorMessage,
                StepsToReproduce = request.ComponentStack ?? "Component stack not available",
                Severity = "High",
                ReporterName = userCtx.Name ?? "Unknown",
                ReporterEmail = userCtx.Email,
                CurrentUrl = request.Url,
                Browser = request.Browser,
                OperatingSystem = "Detected from user agent",
                ScreenSize = "Unknown",
                ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                UserRole = userCtx.SystemRole ?? "Unknown",
                OrgName = userCtx.OrgName ?? "No organisation",
                OrgRole = userCtx.OrgRole ?? "Unknown",
                ExerciseName = "Unknown",
                ExerciseRole = string.Empty,
                AppVersion = "Unknown",
                CommitSha = "Unknown",
            };

            var config = await _emailConfig.GetConfigurationAsync();
            var supportRecipient = new EmailRecipient(config.SupportAddress, "Cadence Support");
            var supportResult = await _emailService.SendTemplatedAsync("BugReport", model, supportRecipient);

            if (supportResult.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "[Feedback] Support email failed for error report {RefNumber}: {Error}",
                    refNumber, supportResult.ErrorMessage);
            }

            _logger.LogInformation(
                "[Feedback] Error report complete - Ref: {RefNumber}, User: {Email}, " +
                "SupportEmailStatus: {SupportStatus}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, supportResult.Status, sw.ElapsedMilliseconds);

            return Ok(new FeedbackResponse(refNumber, "Error report sent to our team. Thank you for helping us improve!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Feedback] Error report FAILED - Ref: {RefNumber}, User: {Email}, ElapsedMs: {ElapsedMs}",
                refNumber, userCtx.Email, sw.ElapsedMilliseconds);
            return StatusCode(500, new { message = "Failed to submit error report. Please try again." });
        }
    }

    /// <summary>
    /// Send acknowledgment email. Best-effort — failures are logged but don't fail the request.
    /// </summary>
    private async Task SendAcknowledgmentSafe(string userName, string userEmail, string refNumber, string ticketType, string title)
    {
        try
        {
            var ackModel = new SupportTicketAcknowledgmentEmailModel
            {
                RecipientName = userName,
                ReferenceNumber = refNumber,
                TicketType = ticketType,
                TicketTitle = title,
                SubmittedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                MessagePreview = $"Thank you for your {ticketType.ToLower()}. Our team will review it shortly.",
            };

            var userRecipient = new EmailRecipient(userEmail, userName);
            var result = await _emailService.SendTemplatedAsync("SupportTicketAcknowledgment", ackModel, userRecipient);

            if (result.Status == EmailSendStatus.Failed)
            {
                _logger.LogWarning(
                    "[Feedback] Acknowledgment email failed for {RefNumber} to {Email}: {Error}",
                    refNumber, userEmail, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Feedback] Acknowledgment email error for {RefNumber} to {Email} (non-critical)",
                refNumber, userEmail);
        }
    }

    /// <summary>
    /// Persist feedback to the database. Best-effort — failures are logged but don't fail the request
    /// since the email has already been sent.
    /// </summary>
    private async Task PersistFeedbackSafe(
        string refNumber,
        FeedbackType type,
        FeedbackUserContext userCtx,
        FeedbackClientContext? clientContext,
        string title,
        string? severity,
        string? contentJson)
    {
        try
        {
            await _feedbackService.SaveAsync(
                refNumber, type,
                userCtx.Email!, userCtx.Name, userCtx.SystemRole, userCtx.OrgName, userCtx.OrgRole,
                clientContext, title, severity, contentJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Feedback] Failed to persist {Type} report {RefNumber} to database (non-critical)",
                type, refNumber);
        }
    }

    /// <summary>
    /// Extracts identity and org context from JWT claims.
    /// Roles and org are sourced here (not trusted from client payload).
    /// </summary>
    private FeedbackUserContext GetCurrentUserContext()
    {
        var email   = User.FindFirstValue(ClaimTypes.Email);
        var name    = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("display_name");
        var role    = User.FindFirstValue("SystemRole") ?? User.FindFirstValue(ClaimTypes.Role);
        var orgName = User.FindFirstValue("org_name");
        var orgRole = User.FindFirstValue("org_role");
        return new FeedbackUserContext(name, email, role, orgName, orgRole);
    }

    private record FeedbackUserContext(
        string? Name,
        string? Email,
        string? SystemRole,
        string? OrgName,
        string? OrgRole);

    /// <summary>Returns " [Role]" when a role is present, or empty string when not in an exercise.</summary>
    private static string FormatExerciseRole(string? role) =>
        string.IsNullOrWhiteSpace(role) ? string.Empty : $" [{role}]";

    private static string GenerateReferenceNumber()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Random.Shared.Next(1000, 9999);
        return $"CAD-{date}-{random}";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
