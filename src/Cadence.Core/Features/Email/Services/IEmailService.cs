using Cadence.Core.Features.Email.Models;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Abstraction for sending emails. Implementations include ACS (production) and Logging (development).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send a raw email message.
    /// </summary>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>
    /// Send a templated email. Renders the template then sends.
    /// </summary>
    Task<EmailSendResult> SendTemplatedAsync<TModel>(
        string templateId,
        TModel model,
        EmailRecipient recipient,
        CancellationToken ct = default);
}
