using Cadence.Core.Features.ExcelExport.Models.DTOs;

namespace Cadence.Core.Features.Eeg.Services;

/// <summary>
/// Service for exporting EEG data for After-Action Review (AAR).
/// </summary>
public interface IEegExportService
{
    /// <summary>
    /// Exports EEG data to Excel format for AAR preparation.
    /// </summary>
    /// <param name="request">Export request with options</param>
    /// <returns>Export result with file content</returns>
    Task<ExportResult> ExportEegDataAsync(ExportEegRequest request);

    /// <summary>
    /// Exports EEG data to JSON format for API integration.
    /// </summary>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="includeEvaluatorNames">Whether to include evaluator names</param>
    /// <returns>JSON export data</returns>
    Task<EegExportJsonDto> ExportEegJsonAsync(Guid exerciseId, bool includeEvaluatorNames = true);
}

/// <summary>
/// Request DTO for EEG export.
/// </summary>
public record ExportEegRequest(
    Guid ExerciseId,
    string Format = "xlsx",
    bool IncludeSummary = true,
    bool IncludeByCapability = true,
    bool IncludeAllEntries = true,
    bool IncludeCoverageGaps = true,
    bool IncludeEvaluatorNames = true,
    bool IncludeFormatting = true,
    string? Filename = null
);

/// <summary>
/// JSON export DTO for EEG data.
/// </summary>
public record EegExportJsonDto
{
    public ExerciseInfoDto Exercise { get; init; } = default!;
    public EegSummaryDto Summary { get; init; } = default!;
    public IEnumerable<CapabilityExportDto> ByCapability { get; init; } = [];
    public IEnumerable<CoverageGapDto> CoverageGaps { get; init; } = [];
    public DateTime GeneratedAt { get; init; }
}

public record ExerciseInfoDto(
    string Name,
    string Date,
    string Status
);

public record EegSummaryDto(
    int TotalEntries,
    TaskCoverageDto TasksCoverage,
    RatingDistributionDto RatingDistribution
);

public record TaskCoverageDto(
    int Evaluated,
    int Total,
    int Percentage
);

public record RatingDistributionDto(
    int P,
    int S,
    int M,
    int U
);

public record CapabilityExportDto
{
    public string CapabilityName { get; init; } = default!;
    public string TargetDescription { get; init; } = default!;
    public IEnumerable<TaskExportDto> Tasks { get; init; } = [];
}

public record TaskExportDto
{
    public string TaskDescription { get; init; } = default!;
    public IEnumerable<EntryExportDto> Entries { get; init; } = [];
}

public record EntryExportDto(
    string Rating,
    string Observation,
    string? Evaluator,
    DateTime ObservedAt
);

public record CoverageGapDto(
    string CapabilityName,
    string TargetDescription,
    string TaskDescription
);
