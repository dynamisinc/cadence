using Cadence.Core.Features.Email.Models;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Renders email templates with dynamic model data.
/// Produces both HTML and plain text versions.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Render a template with the given model data.
    /// </summary>
    /// <typeparam name="TModel">Type of the template model.</typeparam>
    /// <param name="templateId">Template identifier (e.g., "OrganizationInvite", "PasswordReset").</param>
    /// <param name="model">Model containing dynamic data for the template.</param>
    /// <returns>Rendered email with HTML and plain text bodies.</returns>
    Task<RenderedEmail> RenderAsync<TModel>(string templateId, TModel model);
}
