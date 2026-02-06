using System.Security.Claims;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Email.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly EmailServiceOptions _emailOptions;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IEmailService emailService,
        IOptions<EmailServiceOptions> emailOptions,
        ILogger<FeedbackController> logger)
    {
        _emailService = emailService;
        _emailOptions = emailOptions.Value;
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
        var (userName, userEmail) = GetCurrentUser();
        if (userEmail == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();

        var model = new BugReportEmailModel
        {
            Title = request.Title,
            Description = request.Description,
            StepsToReproduce = request.StepsToReproduce ?? "Not provided",
            Severity = request.Severity,
            ReporterName = userName ?? "Unknown",
            ReporterEmail = userEmail,
            CurrentUrl = Request.Headers.Referer.ToString(),
            Browser = Request.Headers.UserAgent.ToString(),
            OperatingSystem = "Detected from user agent",
            ScreenSize = "Unknown",
            ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
        };

        // Send bug report to support
        var supportRecipient = new EmailRecipient(_emailOptions.SupportAddress, "Cadence Support");
        await _emailService.SendTemplatedAsync("BugReport", model, supportRecipient);

        // Send acknowledgment to user
        await SendAcknowledgment(userName ?? "User", userEmail, refNumber, "Bug Report", request.Title);

        _logger.LogInformation("Bug report submitted: {RefNumber} by {Email}", refNumber, userEmail);

        return Ok(new FeedbackResponse(refNumber, "Bug report submitted successfully. You'll receive a confirmation email shortly."));
    }

    /// <summary>
    /// Submit a feature request.
    /// </summary>
    [HttpPost("feature-request")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFeatureRequest([FromBody] SubmitFeatureRequestRequest request)
    {
        var (userName, userEmail) = GetCurrentUser();
        if (userEmail == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();

        var model = new FeatureRequestEmailModel
        {
            Title = request.Title,
            Description = request.Description,
            UseCase = request.UseCase ?? "Not provided",
            ReporterName = userName ?? "Unknown",
            ReporterEmail = userEmail,
            ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
        };

        var supportRecipient = new EmailRecipient(_emailOptions.SupportAddress, "Cadence Support");
        await _emailService.SendTemplatedAsync("FeatureRequest", model, supportRecipient);

        await SendAcknowledgment(userName ?? "User", userEmail, refNumber, "Feature Request", request.Title);

        _logger.LogInformation("Feature request submitted: {RefNumber} by {Email}", refNumber, userEmail);

        return Ok(new FeedbackResponse(refNumber, "Feature request submitted successfully. Thank you for your feedback!"));
    }

    /// <summary>
    /// Submit general feedback.
    /// </summary>
    [HttpPost("general")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitGeneralFeedback([FromBody] SubmitGeneralFeedbackRequest request)
    {
        var (userName, userEmail) = GetCurrentUser();
        if (userEmail == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();

        var model = new GeneralFeedbackEmailModel
        {
            Category = request.Category,
            Subject = request.Subject,
            Message = request.Message,
            SenderName = userName ?? "Unknown",
            SenderEmail = userEmail,
            SentAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
        };

        var supportRecipient = new EmailRecipient(_emailOptions.SupportAddress, "Cadence Support");
        await _emailService.SendTemplatedAsync("GeneralFeedback", model, supportRecipient);

        await SendAcknowledgment(userName ?? "User", userEmail, refNumber, "Feedback", request.Subject);

        _logger.LogInformation("General feedback submitted: {RefNumber} by {Email}", refNumber, userEmail);

        return Ok(new FeedbackResponse(refNumber, "Feedback submitted successfully. Thank you!"));
    }

    /// <summary>
    /// Submit an error report from the ErrorBoundary component.
    /// </summary>
    [HttpPost("error-report")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitErrorReport([FromBody] SubmitErrorReportRequest request)
    {
        var (userName, userEmail) = GetCurrentUser();
        if (userEmail == null) return Unauthorized();

        var refNumber = GenerateReferenceNumber();

        // Use the BugReport template with auto-populated fields for error reports
        var model = new BugReportEmailModel
        {
            Title = $"[Auto] Runtime Error: {Truncate(request.ErrorMessage, 100)}",
            Description = request.ErrorMessage,
            StepsToReproduce = request.ComponentStack ?? "Component stack not available",
            Severity = "High",
            ReporterName = userName ?? "Unknown",
            ReporterEmail = userEmail,
            CurrentUrl = request.Url,
            Browser = request.Browser,
            OperatingSystem = "Detected from user agent",
            ScreenSize = "Unknown",
            ReportedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
        };

        var supportRecipient = new EmailRecipient(_emailOptions.SupportAddress, "Cadence Support");
        await _emailService.SendTemplatedAsync("BugReport", model, supportRecipient);

        _logger.LogInformation("Error report submitted: {RefNumber} by {Email}", refNumber, userEmail);

        return Ok(new FeedbackResponse(refNumber, "Error report sent to our team. Thank you for helping us improve!"));
    }

    private async Task SendAcknowledgment(string userName, string userEmail, string refNumber, string ticketType, string title)
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
        await _emailService.SendTemplatedAsync("SupportTicketAcknowledgment", ackModel, userRecipient);
    }

    private (string? Name, string? Email) GetCurrentUser()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("display_name");
        return (name, email);
    }

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
