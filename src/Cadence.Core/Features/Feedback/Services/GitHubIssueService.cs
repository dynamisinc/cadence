using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cadence.Core.Data;
using Cadence.Core.Features.Feedback.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Feedback.Services;

public class GitHubIssueService : IGitHubIssueService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubIssueService> _logger;

    public GitHubIssueService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<GitHubIssueService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(int IssueNumber, string IssueUrl)?> CreateIssueAsync(
        string referenceNumber,
        FeedbackType type,
        string title,
        string? severity,
        string? contentJson,
        string reporterEmail,
        string? reporterName,
        string? orgName)
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.GitHubToken) ||
            string.IsNullOrEmpty(settings.GitHubOwner) || string.IsNullOrEmpty(settings.GitHubRepo))
        {
            return null;
        }

        var client = CreateClient(settings.GitHubToken);
        var url = $"repos/{settings.GitHubOwner}/{settings.GitHubRepo}/issues";

        var body = BuildIssueBody(referenceNumber, type, title, severity, contentJson,
            reporterEmail, reporterName, orgName);

        var labels = settings.GitHubLabelsEnabled ? GetLabels(type) : Array.Empty<string>();

        var issueRequest = new
        {
            title = $"[{referenceNumber}] {FormatTypePrefix(type)}: {title}",
            body,
            labels,
        };

        var json = JsonSerializer.Serialize(issueRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "[GitHub] Failed to create issue for {RefNumber}: {StatusCode} {Error}",
                referenceNumber, response.StatusCode, errorBody);
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var issue = JsonSerializer.Deserialize<GitHubIssueResponse>(responseJson);

        if (issue == null)
            return null;

        _logger.LogInformation(
            "[GitHub] Created issue #{IssueNumber} for {RefNumber}: {Url}",
            issue.Number, referenceNumber, issue.HtmlUrl);

        return (issue.Number, issue.HtmlUrl);
    }

    public async Task CloseIssueAsync(int issueNumber, string? comment = null)
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.GitHubToken) ||
            string.IsNullOrEmpty(settings.GitHubOwner) || string.IsNullOrEmpty(settings.GitHubRepo))
            return;

        try
        {
            var client = CreateClient(settings.GitHubToken);
            var url = $"repos/{settings.GitHubOwner}/{settings.GitHubRepo}/issues/{issueNumber}";

            if (!string.IsNullOrEmpty(comment))
                await AddCommentInternal(client, settings.GitHubOwner, settings.GitHubRepo, issueNumber, comment);

            var payload = JsonSerializer.Serialize(new { state = "closed" });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PatchAsync(url, content);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("[GitHub] Closed issue #{IssueNumber}", issueNumber);
            else
                _logger.LogWarning("[GitHub] Failed to close issue #{IssueNumber}: {StatusCode}", issueNumber, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHub] Failed to close issue #{IssueNumber} (non-critical)", issueNumber);
        }
    }

    public async Task AddIssueCommentAsync(int issueNumber, string comment)
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.GitHubToken) ||
            string.IsNullOrEmpty(settings.GitHubOwner) || string.IsNullOrEmpty(settings.GitHubRepo))
            return;

        try
        {
            var client = CreateClient(settings.GitHubToken);
            await AddCommentInternal(client, settings.GitHubOwner, settings.GitHubRepo, issueNumber, comment);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHub] Failed to add comment to issue #{IssueNumber} (non-critical)", issueNumber);
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings == null || string.IsNullOrEmpty(settings.GitHubToken))
            return (false, "GitHub token is not configured.");

        if (string.IsNullOrEmpty(settings.GitHubOwner) || string.IsNullOrEmpty(settings.GitHubRepo))
            return (false, "Repository owner and name must be configured.");

        var client = CreateClient(settings.GitHubToken);
        var url = $"repos/{settings.GitHubOwner}/{settings.GitHubRepo}";

        try
        {
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var repo = JsonSerializer.Deserialize<GitHubRepoResponse>(json);
                var permissions = repo?.Permissions;

                if (permissions is { Push: true })
                    return (true, $"Connected to {settings.GitHubOwner}/{settings.GitHubRepo} with write access.");

                return (true, $"Connected to {settings.GitHubOwner}/{settings.GitHubRepo} (read-only — issues may require push access).");
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => (false, "Invalid token — authentication failed."),
                System.Net.HttpStatusCode.Forbidden => (false, "Token lacks required permissions for this repository."),
                System.Net.HttpStatusCode.NotFound => (false, $"Repository '{settings.GitHubOwner}/{settings.GitHubRepo}' not found or not accessible."),
                _ => (false, $"GitHub API returned {(int)response.StatusCode}: {response.ReasonPhrase}"),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GitHub] Connection test failed");
            return (false, $"Connection failed: {ex.Message}");
        }
    }

    private async Task AddCommentInternal(HttpClient client, string owner, string repo, int issueNumber, string comment)
    {
        var url = $"repos/{owner}/{repo}/issues/{issueNumber}/comments";
        var payload = JsonSerializer.Serialize(new { body = comment });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
            _logger.LogInformation("[GitHub] Added comment to issue #{IssueNumber}", issueNumber);
        else
            _logger.LogWarning("[GitHub] Failed to add comment to issue #{IssueNumber}: {StatusCode}", issueNumber, response.StatusCode);
    }

    private HttpClient CreateClient(string token)
    {
        var client = _httpClientFactory.CreateClient("github");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string BuildIssueBody(
        string referenceNumber,
        FeedbackType type,
        string title,
        string? severity,
        string? contentJson,
        string reporterEmail,
        string? reporterName,
        string? orgName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### {FormatTypePrefix(type)}: {title}");
        sb.AppendLine();
        sb.AppendLine($"**Reference:** {referenceNumber}");

        if (severity != null)
            sb.AppendLine($"**Severity:** {severity}");

        var reporter = reporterName != null ? $"{reporterName} ({reporterEmail})" : reporterEmail;
        sb.AppendLine($"**Reporter:** {reporter}");

        if (orgName != null)
            sb.AppendLine($"**Organization:** {orgName}");

        if (!string.IsNullOrEmpty(contentJson))
        {
            sb.AppendLine();
            try
            {
                var doc = JsonDocument.Parse(contentJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var value = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        sb.AppendLine($"#### {FormatPropertyName(prop.Name)}");
                        sb.AppendLine(value);
                        sb.AppendLine();
                    }
                }
            }
            catch
            {
                sb.AppendLine("#### Details");
                sb.AppendLine(contentJson);
            }
        }

        sb.AppendLine("---");
        sb.AppendLine("*Created automatically by Cadence feedback system*");

        return sb.ToString();
    }

    private static string FormatTypePrefix(FeedbackType type) => type switch
    {
        FeedbackType.BugReport => "Bug Report",
        FeedbackType.FeatureRequest => "Feature Request",
        FeedbackType.General => "General Feedback",
        _ => "Feedback",
    };

    private static string FormatPropertyName(string name)
    {
        // Convert camelCase to Title Case with spaces
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpper(name[i]) : name[i]);
        }
        return sb.ToString();
    }

    private static string[] GetLabels(FeedbackType type) => type switch
    {
        FeedbackType.BugReport => ["bug"],
        FeedbackType.FeatureRequest => ["enhancement"],
        FeedbackType.General => ["feedback"],
        _ => [],
    };

    private record GitHubIssueResponse
    {
        [JsonPropertyName("number")]
        public int Number { get; init; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;
    }

    private record GitHubRepoResponse
    {
        [JsonPropertyName("permissions")]
        public GitHubRepoPermissions? Permissions { get; init; }
    }

    private record GitHubRepoPermissions
    {
        [JsonPropertyName("push")]
        public bool Push { get; init; }
    }
}
