using Cadence.Core.Features.ExcelExport.Models.DTOs;

namespace Cadence.Core.Features.ExcelExport.Services;

/// <summary>
/// Service for exporting MSEL data to Excel format.
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
    /// Generates a blank MSEL template for data entry.
    /// </summary>
    /// <param name="includeFormatting">Whether to include formatting</param>
    /// <returns>Export result with template file</returns>
    Task<ExportResult> GenerateTemplateAsync(bool includeFormatting = true);
}
