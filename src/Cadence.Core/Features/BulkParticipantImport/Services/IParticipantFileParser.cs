using Cadence.Core.Features.BulkParticipantImport.Models.DTOs;

namespace Cadence.Core.Features.BulkParticipantImport.Services;

/// <summary>
/// Parses CSV and XLSX files containing participant data.
/// Handles column synonym detection, row validation, and flexible header matching.
/// </summary>
public interface IParticipantFileParser
{
    /// <summary>
    /// Parses an uploaded file stream and returns structured row data with validation.
    /// </summary>
    /// <param name="fileStream">The uploaded file content.</param>
    /// <param name="fileName">Original file name (used for extension detection).</param>
    /// <returns>Parse result with rows, column mappings, and any errors.</returns>
    Task<FileParseResult> ParseAsync(Stream fileStream, string fileName);
}
