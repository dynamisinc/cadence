namespace Cadence.Core.Features.Eeg.Models.DTOs;

/// <summary>
/// Document generation mode.
/// </summary>
public enum EegDocumentMode
{
    /// <summary>
    /// Blank EEG for evaluators to use during conduct.
    /// </summary>
    Blank,

    /// <summary>
    /// Completed EEG with observations and ratings.
    /// </summary>
    Completed
}

/// <summary>
/// Output format for generated documents.
/// </summary>
public enum EegDocumentOutputFormat
{
    /// <summary>
    /// Single document containing all capabilities.
    /// </summary>
    Single,

    /// <summary>
    /// Separate document per capability (ZIP download).
    /// </summary>
    PerCapability
}

/// <summary>
/// Request DTO for generating an EEG document.
/// </summary>
public class GenerateEegDocumentRequest
{
    /// <summary>
    /// Document mode: blank or completed.
    /// </summary>
    public EegDocumentMode Mode { get; init; } = EegDocumentMode.Blank;

    /// <summary>
    /// Output format: single document or per-capability ZIP.
    /// </summary>
    public EegDocumentOutputFormat OutputFormat { get; init; } = EegDocumentOutputFormat.Single;

    /// <summary>
    /// For completed mode: whether to include evaluator names.
    /// </summary>
    public bool IncludeEvaluatorNames { get; init; } = true;
}

/// <summary>
/// Result of document generation.
/// </summary>
public record EegDocumentResult(
    /// <summary>
    /// The generated document content.
    /// </summary>
    byte[] Content,

    /// <summary>
    /// The MIME content type.
    /// </summary>
    string ContentType,

    /// <summary>
    /// The suggested filename.
    /// </summary>
    string Filename,

    /// <summary>
    /// Number of capability targets in the document.
    /// </summary>
    int CapabilityTargetCount,

    /// <summary>
    /// Number of critical tasks in the document.
    /// </summary>
    int CriticalTaskCount
);
