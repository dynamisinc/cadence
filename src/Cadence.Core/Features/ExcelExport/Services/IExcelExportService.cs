using Cadence.Core.Features.ExcelExport.Models.DTOs;

namespace Cadence.Core.Features.ExcelExport.Services;

/// <summary>
/// Service for exporting MSEL and exercise data to Excel format.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Exports the MSEL for an exercise to Excel or CSV format.
    /// </summary>
    /// <param name="request">Export request with options</param>
    /// <returns>Export result with file content</returns>
    Task<ExportResult> ExportMselAsync(ExportMselRequest request);

    /// <summary>
    /// Exports observations for an exercise to Excel format.
    /// </summary>
    /// <param name="request">Export request with options</param>
    /// <returns>Export result with file content</returns>
    Task<ExportResult> ExportObservationsAsync(ExportObservationsRequest request);

    /// <summary>
    /// Exports full exercise package as a ZIP file containing MSEL, Observations, and Summary.
    /// </summary>
    /// <param name="request">Export request with options</param>
    /// <returns>Export result with ZIP file content</returns>
    Task<ExportResult> ExportFullPackageAsync(ExportFullPackageRequest request);

    /// <summary>
    /// Generates a blank MSEL template for data entry.
    /// </summary>
    /// <param name="includeFormatting">Whether to include formatting</param>
    /// <returns>Export result with template file</returns>
    Task<ExportResult> GenerateTemplateAsync(bool includeFormatting = true);
}
