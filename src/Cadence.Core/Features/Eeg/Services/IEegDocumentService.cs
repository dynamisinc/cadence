using Cadence.Core.Features.Eeg.Models.DTOs;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service interface for generating EEG documents (HSEEP-compliant Word documents).
/// </summary>
public interface IEegDocumentService
{
    /// <summary>
    /// Generate an EEG document for an exercise.
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Document generation options</param>
    /// <returns>The generated document result</returns>
    Task<EegDocumentResult> GenerateAsync(Guid exerciseId, GenerateEegDocumentRequest request);
}
