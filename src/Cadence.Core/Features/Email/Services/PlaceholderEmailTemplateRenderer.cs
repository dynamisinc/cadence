using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Cadence.Core.Features.Email.Models;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Simple placeholder-based email template renderer.
/// Uses {{PropertyName}} syntax for variable substitution.
/// Templates are loaded from embedded resources or the file system.
/// </summary>
public partial class PlaceholderEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly ILogger<PlaceholderEmailTemplateRenderer> _logger;
    private readonly IEmailTemplateStore _templateStore;

    public PlaceholderEmailTemplateRenderer(
        ILogger<PlaceholderEmailTemplateRenderer> logger,
        IEmailTemplateStore templateStore)
    {
        _logger = logger;
        _templateStore = templateStore;
    }

    public async Task<RenderedEmail> RenderAsync<TModel>(string templateId, TModel model)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("Template ID is required.", nameof(templateId));
        }

        var template = await _templateStore.GetTemplateAsync(templateId);
        if (template == null)
        {
            throw new InvalidOperationException($"Email template '{templateId}' not found.");
        }

        var htmlBody = ReplacePlaceholders(template.HtmlContent, model);
        var plainTextBody = ReplacePlaceholders(template.PlainTextContent, model);
        var subject = ReplacePlaceholders(template.SubjectTemplate, model);

        // Wrap HTML in layout if available
        var layout = await _templateStore.GetTemplateAsync("_Layout");
        if (layout != null)
        {
            htmlBody = layout.HtmlContent.Replace("{{Content}}", htmlBody);
        }

        _logger.LogDebug("Rendered email template '{TemplateId}' with model type {ModelType}",
            templateId, typeof(TModel).Name);

        return new RenderedEmail(subject, htmlBody, plainTextBody);
    }

    private static string ReplacePlaceholders<TModel>(string content, TModel model)
    {
        if (string.IsNullOrEmpty(content) || model == null)
        {
            return content ?? string.Empty;
        }

        var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(model);
            var placeholder = $"{{{{{prop.Name}}}}}";
            var replacement = FormatValue(value);
            content = content.Replace(placeholder, replacement);
        }

        // Remove any remaining unreplaced placeholders
        content = UnmatchedPlaceholderRegex().Replace(content, string.Empty);

        return content;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    [GeneratedRegex(@"\{\{[^}]+\}\}")]
    private static partial Regex UnmatchedPlaceholderRegex();
}

/// <summary>
/// Represents a raw email template (before rendering).
/// </summary>
public record EmailTemplate(
    string TemplateId,
    string SubjectTemplate,
    string HtmlContent,
    string PlainTextContent
);

/// <summary>
/// Store for loading email templates.
/// </summary>
public interface IEmailTemplateStore
{
    /// <summary>
    /// Get a template by its ID.
    /// </summary>
    Task<EmailTemplate?> GetTemplateAsync(string templateId);
}

/// <summary>
/// In-memory template store. Templates are registered at startup.
/// </summary>
public class InMemoryEmailTemplateStore : IEmailTemplateStore
{
    private readonly Dictionary<string, EmailTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a template.
    /// </summary>
    public void AddTemplate(EmailTemplate template)
    {
        _templates[template.TemplateId] = template;
    }

    public Task<EmailTemplate?> GetTemplateAsync(string templateId)
    {
        _templates.TryGetValue(templateId, out var template);
        return Task.FromResult(template);
    }
}
