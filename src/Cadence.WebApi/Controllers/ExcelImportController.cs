using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for Excel import operations.
/// </summary>
[ApiController]
[Route("api/import")]
public class ExcelImportController : ControllerBase
{
    private readonly IExcelImportService _service;
    private readonly ILogger<ExcelImportController> _logger;

    private static readonly string[] SupportedExtensions = { ".xlsx", ".xls", ".csv" };
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ExcelImportController(
        IExcelImportService service,
        ILogger<ExcelImportController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Upload and analyze an Excel file for import.
    /// </summary>
    /// <param name="file">The Excel file to analyze</param>
    /// <returns>File analysis result with worksheet information</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(FileAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileAnalysisResultDto>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"Unsupported file format: {extension}",
                supportedFormats = SupportedExtensions
            });
        }

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
        {
            return BadRequest(new
            {
                message = $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB"
            });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _service.AnalyzeFileAsync(file.FileName, stream);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded file");
            return BadRequest(new { message = "Failed to process the uploaded file. Please ensure it is a valid Excel file." });
        }
    }

    /// <summary>
    /// Get the current state of an import session.
    /// </summary>
    /// <param name="sessionId">The import session ID</param>
    /// <returns>Session state or 404 if not found</returns>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ImportSessionStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportSessionStateDto>> GetSessionState(Guid sessionId)
    {
        var session = await _service.GetSessionStateAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Import session not found or has expired" });
        }

        return Ok(session);
    }

    /// <summary>
    /// Select a worksheet for import and get column information.
    /// </summary>
    /// <param name="request">Worksheet selection request</param>
    /// <returns>Worksheet details with column information</returns>
    [HttpPost("select-worksheet")]
    [ProducesResponseType(typeof(WorksheetSelectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorksheetSelectionResultDto>> SelectWorksheet([FromBody] SelectWorksheetRequestDto request)
    {
        try
        {
            var result = await _service.SelectWorksheetAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error selecting worksheet: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get suggested column mappings based on column headers.
    /// </summary>
    /// <param name="sessionId">The import session ID</param>
    /// <returns>List of suggested column mappings</returns>
    [HttpGet("sessions/{sessionId:guid}/mappings")]
    [ProducesResponseType(typeof(IReadOnlyList<ColumnMappingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ColumnMappingDto>>> GetSuggestedMappings(Guid sessionId)
    {
        try
        {
            var mappings = await _service.GetSuggestedMappingsAsync(sessionId);
            return Ok(mappings);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error getting suggested mappings: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validate import data with configured mappings.
    /// </summary>
    /// <param name="request">Mapping configuration</param>
    /// <returns>Validation results</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ValidationResultDto>> ValidateImport([FromBody] ConfigureMappingsRequestDto request)
    {
        try
        {
            var result = await _service.ValidateImportAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error validating import: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Execute the import with configured mappings.
    /// </summary>
    /// <param name="request">Import execution request</param>
    /// <returns>Import result</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResultDto>> ExecuteImport([FromBody] ExecuteImportRequestDto request)
    {
        try
        {
            var result = await _service.ExecuteImportAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error executing import: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel an import session and clean up temporary files.
    /// </summary>
    /// <param name="sessionId">The import session ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelImport(Guid sessionId)
    {
        await _service.CancelImportAsync(sessionId);
        return NoContent();
    }
}
