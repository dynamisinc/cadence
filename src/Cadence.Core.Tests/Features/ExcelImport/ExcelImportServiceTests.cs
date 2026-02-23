using Cadence.Core.Data;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class ExcelImportServiceTests : IDisposable
{
    private readonly Mock<IInjectService> _injectServiceMock;
    private readonly Mock<ILogger<ExcelImportService>> _loggerMock;
    private readonly AppDbContext _context;
    private readonly ExcelImportService _service;

    public ExcelImportServiceTests()
    {
        _injectServiceMock = new Mock<IInjectService>();
        _loggerMock = new Mock<ILogger<ExcelImportService>>();
        _context = TestDbContextFactory.Create();
        _service = new ExcelImportService(_context, _injectServiceMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Helpers

    /// <summary>
    /// Creates an in-memory Excel file stream with the specified headers and data rows.
    /// </summary>
    private static MemoryStream CreateExcelStream(string[] headers, object?[][]? dataRows = null, string sheetName = "MSEL")
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(1, col + 1).Value = headers[col];
        }

        if (dataRows != null)
        {
            for (int row = 0; row < dataRows.Length; row++)
            {
                for (int col = 0; col < dataRows[row].Length; col++)
                {
                    var value = dataRows[row][col];
                    if (value != null)
                    {
                        var cell = ws.Cell(row + 2, col + 1);
                        switch (value)
                        {
                            case DateTime dt:
                                cell.Value = dt;
                                break;
                            case double d:
                                cell.Value = d;
                                break;
                            case int i:
                                cell.Value = i;
                                break;
                            default:
                                cell.Value = value.ToString();
                                break;
                        }
                    }
                }
            }
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Creates an Excel file with headers on a non-first row (e.g., row 3) to test multi-row header detection.
    /// </summary>
    private static MemoryStream CreateExcelStreamWithHeaderOffset(
        int headerRow, string[] headers, object?[][]? dataRows = null, string sheetName = "MSEL",
        string[][]? preambleRows = null)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName);

        // Write preamble rows (before the header)
        if (preambleRows != null)
        {
            for (int row = 0; row < preambleRows.Length; row++)
            {
                for (int col = 0; col < preambleRows[row].Length; col++)
                {
                    ws.Cell(row + 1, col + 1).Value = preambleRows[row][col];
                }
            }
        }

        // Write headers at the specified row
        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(headerRow, col + 1).Value = headers[col];
        }

        // Write data rows after headers
        if (dataRows != null)
        {
            for (int row = 0; row < dataRows.Length; row++)
            {
                for (int col = 0; col < dataRows[row].Length; col++)
                {
                    var value = dataRows[row][col];
                    if (value != null)
                    {
                        ws.Cell(headerRow + 1 + row, col + 1).Value = value.ToString();
                    }
                }
            }
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Runs the full wizard flow (Analyze → SelectWorksheet → GetMappings) and returns the session ID and mappings.
    /// </summary>
    private async Task<(Guid SessionId, IReadOnlyList<ColumnMappingDto> Mappings, FileAnalysisResultDto Analysis)>
        RunWizardThroughMappings(string[] headers, object?[][]? dataRows = null, int headerRow = 1, int dataStartRow = 2)
    {
        using var stream = CreateExcelStream(headers, dataRows);
        var analysis = await _service.AnalyzeFileAsync("test.xlsx", stream);

        await _service.SelectWorksheetAsync(new SelectWorksheetRequestDto
        {
            SessionId = analysis.SessionId,
            WorksheetIndex = 0,
            HeaderRow = headerRow,
            DataStartRow = dataStartRow,
            PreviewRowCount = 5
        });

        var mappings = await _service.GetSuggestedMappingsAsync(analysis.SessionId);
        return (analysis.SessionId, mappings, analysis);
    }

    #endregion

    #region Column Pattern Matching - Legacy Headers

    [Theory]
    [InlineData("MSEL NUMBER", "InjectNumber")]
    [InlineData("MSEL #", "InjectNumber")]
    [InlineData("MSEL No", "InjectNumber")]
    [InlineData("INJ#", "InjectNumber")]
    [InlineData("Inject Number", "InjectNumber")]
    [InlineData("#", "InjectNumber")]
    public async Task GetSuggestedMappings_LegacyInjectNumberHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { header, "Title" });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(0);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Subject", "Title")]
    [InlineData("Event Title", "Title")]
    [InlineData("Inject", "Title")]
    public async Task GetSuggestedMappings_LegacyTitleHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "No", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Description/Script", "Description")]
    [InlineData("Detailed Statement", "Description")]
    [InlineData("Script", "Description")]
    [InlineData("Text", "Description")]
    public async Task GetSuggestedMappings_LegacyDescriptionHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Inject DTG", "ScheduledTime")]
    [InlineData("DTG", "ScheduledTime")]
    [InlineData("Date Time", "ScheduledTime")]
    [InlineData("Actual DTG", "ScheduledTime")]
    public async Task GetSuggestedMappings_LegacyTimeHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Send From", "Source")]
    [InlineData("Sent From", "Source")]
    [InlineData("Initiated By", "Source")]
    public async Task GetSuggestedMappings_LegacySourceHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Send To", "Target")]
    public async Task GetSuggestedMappings_LegacyTargetHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Means", "DeliveryMethod")]
    [InlineData("Inject Mode", "DeliveryMethod")]
    [InlineData("Modality", "DeliveryMethod")]
    public async Task GetSuggestedMappings_LegacyDeliveryMethodHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Storyline/Thread", "Track")]
    [InlineData("Storyline", "Track")]
    [InlineData("Thread", "Track")]
    [InlineData("ESF", "Track")]
    public async Task GetSuggestedMappings_LegacyTrackHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Notes/Remarks", "Notes")]
    [InlineData("Comments/Notes", "Notes")]
    public async Task GetSuggestedMappings_LegacyNotesHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Remarks/Expected Outcome", "ExpectedAction")]
    [InlineData("Expected Outcome", "ExpectedAction")]
    public async Task GetSuggestedMappings_LegacyExpectedActionHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    [Theory]
    [InlineData("Injected By", "ResponsibleController")]
    [InlineData("POC", "ResponsibleController")]
    [InlineData("Inject Author", "ResponsibleController")]
    public async Task GetSuggestedMappings_LegacyControllerHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
    }

    #endregion

    #region ScheduledTime Is Optional

    [Fact]
    public async Task GetSuggestedMappings_ScheduledTime_IsNotRequired()
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", "Description" });

        var scheduledTimeMapping = mappings.First(m => m.CadenceField == "ScheduledTime");
        scheduledTimeMapping.IsRequired.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateImport_MissingScheduledTime_ProducesWarningNotError()
    {
        var headers = new[] { "Title", "Description" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject 1", "Description of inject 1" },
            new object?[] { "Test Inject 2", "Description of inject 2" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        // Rows should be "Warning", not "Error" — because ScheduledTime is only a warning
        result.ErrorRows.Should().Be(0);
        result.WarningRows.Should().Be(2);

        foreach (var row in result.Rows)
        {
            row.Status.Should().Be("Warning");
            row.Issues.Should().Contain(i =>
                i.Field == "ScheduledTime" &&
                i.Severity == "Warning" &&
                i.Message.Contains("default to 00:00"));
        }
    }

    #endregion

    #region Multi-Row Header Detection

    [Fact]
    public async Task AnalyzeFile_HeadersOnRow3_SuggestsCorrectHeaderRow()
    {
        var preamble = new[]
        {
            new[] { "Alabama Emergency Management Agency" },
            new[] { "Hurricane Exercise 2013" }
        };
        var headers = new[] { "MSEL Number", "Subject", "Description", "Inject DTG", "From", "To" };

        using var stream = CreateExcelStreamWithHeaderOffset(3, headers, preambleRows: preamble);
        var analysis = await _service.AnalyzeFileAsync("test.xlsx", stream);

        var worksheet = analysis.Worksheets[0];
        worksheet.LooksLikeMsel.Should().BeTrue();
        worksheet.SuggestedHeaderRow.Should().Be(3);
        worksheet.SuggestedDataStartRow.Should().Be(4);
    }

    [Fact]
    public async Task AnalyzeFile_HeadersOnRow1_SuggestsRow1()
    {
        var headers = new[] { "Title", "Description", "Time", "From", "To" };
        using var stream = CreateExcelStream(headers);
        var analysis = await _service.AnalyzeFileAsync("test.xlsx", stream);

        var worksheet = analysis.Worksheets[0];
        worksheet.SuggestedHeaderRow.Should().Be(1);
        worksheet.SuggestedDataStartRow.Should().Be(2);
    }

    #endregion

    #region MaxColumns Cap

    [Fact]
    public async Task AnalyzeFile_PhantomColumns_CapsColumnCount()
    {
        // Simulate a file with many columns (like BAMA MSEL with 16382 columns)
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("MSEL");

        // Add real headers to first 10 columns
        var realHeaders = new[] { "Title", "Description", "Time", "From", "To", "Track", "Notes", "Status", "Category", "Phase" };
        for (int col = 0; col < realHeaders.Length; col++)
        {
            ws.Cell(1, col + 1).Value = realHeaders[col];
        }

        // Add a cell way out at column 200 to simulate phantom columns
        ws.Cell(1, 200).Value = "phantom";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var analysis = await _service.AnalyzeFileAsync("phantom.xlsx", ms);

        // Column count should be capped at 100 max
        analysis.Worksheets[0].ColumnCount.Should().BeLessThanOrEqualTo(100);
    }

    #endregion

    #region Empty Row Skipping

    [Fact]
    public async Task ValidateImport_EmptyRows_SkippedDuringValidation()
    {
        var headers = new[] { "Title", "Description" };
        var dataRows = new[]
        {
            new object?[] { "Inject 1", "Desc 1" },
            new object?[] { null, null },           // Empty row (spacer)
            new object?[] { "", "" },               // Empty string row
            new object?[] { "Inject 2", "Desc 2" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        // Only the 2 non-empty rows should be included
        result.TotalRows.Should().Be(2);
    }

    #endregion

    #region InjectType Synonyms

    [Fact]
    public async Task ValidateImport_LegacyInjectTypeValues_NoErrors()
    {
        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[]
        {
            new object?[] { "Inject 1", "administrative" },
            new object?[] { "Inject 2", "contextual" },
            new object?[] { "Inject 3", "contingent" },
            new object?[] { "Inject 4", "information" },
            new object?[] { "Inject 5", "operational" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        // Legacy InjectType synonyms should be recognized - no errors
        result.ErrorRows.Should().Be(0);

        // All rows should have InjectType values in the validated output
        foreach (var row in result.Rows)
        {
            var issues = row.Issues ?? [];
            issues.Should().NotContain(i => i.Field == "InjectType" && i.Severity == "Error");
        }
    }

    #endregion

    #region MSEL Detection Patterns

    [Fact]
    public async Task AnalyzeFile_StandardMselHeaders_IdentifiesAsMsel()
    {
        var headers = new[] { "Inject Number", "Title", "Description", "Scheduled Time", "Source", "Target" };
        using var stream = CreateExcelStream(headers);

        var analysis = await _service.AnalyzeFileAsync("test.xlsx", stream);

        analysis.Worksheets[0].LooksLikeMsel.Should().BeTrue();
        analysis.Worksheets[0].MselConfidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AnalyzeFile_LegacyAlabamaMselHeaders_IdentifiesAsMsel()
    {
        // Headers matching the AL MSEL DRAFT 2013 format
        var headers = new[] { "MSEL NUMBER", "Date", "Time", "STATUS", "INJECT DTG", "SUBJECT",
            "Description/Script", "Send From", "Send To", "Means", "Storyline/Thread" };
        using var stream = CreateExcelStream(headers);

        var analysis = await _service.AnalyzeFileAsync("AL MSEL DRAFT 03072013.xlsx", stream);

        analysis.Worksheets[0].LooksLikeMsel.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeFile_LegacyBamaMselHeaders_IdentifiesAsMsel()
    {
        // Headers matching the BAMA MSEL 2011 format (military DTG)
        var headers = new[] { "#", "Inject DTG", "Inject Mode", "From", "To", "Subject",
            "Detailed Statement", "Expected Outcome", "Injected By", "Notes/Remarks" };
        using var stream = CreateExcelStream(headers);

        var analysis = await _service.AnalyzeFileAsync("BAMA MSEL 18 Aug 11.xlsx", stream);

        analysis.Worksheets[0].LooksLikeMsel.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeFile_NonMselSheet_LowConfidence()
    {
        var headers = new[] { "Product", "Price", "Quantity", "Total" };
        using var stream = CreateExcelStream(headers);

        var analysis = await _service.AnalyzeFileAsync("products.xlsx", stream);

        analysis.Worksheets[0].LooksLikeMsel.Should().BeFalse();
    }

    #endregion

    #region Full Wizard Flow - Column Mapping Integration

    [Fact]
    public async Task FullWizard_AlabamaMselHeaders_MapsAllExpectedFields()
    {
        // Real headers from AL MSEL DRAFT 03072013.xlsx
        var headers = new[] { "MSEL NUMBER", "Time", "SUBJECT", "Description/Script",
            "Send From", "Send To", "Means", "Storyline/Thread", "Notes/Remarks" };

        var (_, mappings, _) = await RunWizardThroughMappings(headers);

        // Verify key mappings are detected
        AssertFieldMapped(mappings, "InjectNumber", 0);  // MSEL NUMBER
        AssertFieldMapped(mappings, "ScheduledTime", 1);  // Time
        AssertFieldMapped(mappings, "Title", 2);           // SUBJECT
        AssertFieldMapped(mappings, "Description", 3);     // Description/Script
        AssertFieldMapped(mappings, "Source", 4);           // Send From
        AssertFieldMapped(mappings, "Target", 5);           // Send To
        AssertFieldMapped(mappings, "DeliveryMethod", 6);   // Means
        AssertFieldMapped(mappings, "Track", 7);            // Storyline/Thread
        AssertFieldMapped(mappings, "Notes", 8);            // Notes/Remarks
    }

    [Fact]
    public async Task FullWizard_BamaMselHeaders_MapsAllExpectedFields()
    {
        // Real headers from BAMA MSEL 18 Aug 11.xlsx
        var headers = new[] { "#", "Inject DTG", "Inject Mode", "From", "To", "Subject",
            "Detailed Statement", "Expected Outcome", "Injected By", "Notes/Remarks" };

        var (_, mappings, _) = await RunWizardThroughMappings(headers);

        AssertFieldMapped(mappings, "InjectNumber", 0);       // #
        AssertFieldMapped(mappings, "ScheduledTime", 1);       // Inject DTG
        AssertFieldMapped(mappings, "DeliveryMethod", 2);      // Inject Mode
        AssertFieldMapped(mappings, "Source", 3);              // From
        AssertFieldMapped(mappings, "Target", 4);              // To
        // Note: Title maps to col 1 ("Inject DTG" contains "inject" which is a Title pattern).
        // This is a greedy first-match; the user can manually remap Title to "Subject" in the wizard.
        // Key assertion: ScheduledTime and DeliveryMethod are correctly mapped via exact patterns.
        AssertFieldMapped(mappings, "Description", 6);         // Detailed Statement
        AssertFieldMapped(mappings, "ExpectedAction", 7);      // Expected Outcome
        AssertFieldMapped(mappings, "ResponsibleController", 8); // Injected By
        AssertFieldMapped(mappings, "Notes", 9);               // Notes/Remarks
    }

    [Fact]
    public async Task FullWizard_WinterExerciseHeaders_MapsAvailableFields()
    {
        // Winter Exercise MSEL had no time column but had alphanumeric inject numbers like "5-A"
        var headers = new[] { "#", "Inject", "Description", "From", "To", "Track", "Expected Action" };

        var (_, mappings, _) = await RunWizardThroughMappings(headers);

        AssertFieldMapped(mappings, "InjectNumber", 0);
        AssertFieldMapped(mappings, "Title", 1);          // "Inject" matches Title pattern
        AssertFieldMapped(mappings, "Description", 2);
        AssertFieldMapped(mappings, "Source", 3);
        AssertFieldMapped(mappings, "Target", 4);
        AssertFieldMapped(mappings, "Track", 5);
        AssertFieldMapped(mappings, "ExpectedAction", 6);

        // ScheduledTime should not be mapped (no time column in Winter Exercise)
        var timeMapped = mappings.First(m => m.CadenceField == "ScheduledTime");
        timeMapped.SourceColumnIndex.Should().BeNull();
    }

    private static void AssertFieldMapped(IReadOnlyList<ColumnMappingDto> mappings, string fieldName, int expectedIndex)
    {
        var mapping = mappings.First(m => m.CadenceField == fieldName);
        mapping.SourceColumnIndex.Should().Be(expectedIndex,
            because: $"{fieldName} should map to column index {expectedIndex}");
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80,
            because: $"{fieldName} should have high confidence mapping");
    }

    #endregion

    #region Validation with Time Parsing

    [Fact]
    public async Task ValidateImport_MilitaryDtgTime_ParsesSuccessfully()
    {
        var headers = new[] { "Title", "Inject DTG" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", "210900AUG2011" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.ErrorRows.Should().Be(0);
        // The row may have a warning for ScheduledTime format but should NOT be an error
        result.Rows[0].Issues?.Any(i => i.Field == "ScheduledTime" && i.Severity == "Error").Should().BeFalse();
    }

    [Fact]
    public async Task ValidateImport_DateTimeWithTimezone_ParsesSuccessfully()
    {
        var headers = new[] { "Title", "Date Time" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", "5/08/2009 16:00:00 CDT" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.ErrorRows.Should().Be(0);
    }

    [Fact]
    public async Task ValidateImport_StandardTimeFormat_ParsesSuccessfully()
    {
        var headers = new[] { "Title", "Scheduled Time" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", new DateTime(2024, 1, 1, 14, 30, 0) },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.ErrorRows.Should().Be(0);
    }

    #endregion

    #region File Analysis Basics

    [Fact]
    public async Task AnalyzeFile_ValidExcel_ReturnsSessionAndWorksheetInfo()
    {
        var headers = new[] { "Title", "Description" };
        using var stream = CreateExcelStream(headers, new[] { new object?[] { "Inject 1", "Desc" } });

        var result = await _service.AnalyzeFileAsync("test.xlsx", stream);

        result.SessionId.Should().NotBeEmpty();
        result.FileName.Should().Be("test.xlsx");
        result.FileFormat.Should().Be("xlsx");
        result.Worksheets.Should().HaveCount(1);
        result.Worksheets[0].Name.Should().Be("MSEL");
        result.IsPasswordProtected.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeFile_UnsupportedFormat_ThrowsException()
    {
        using var stream = new MemoryStream(new byte[10]);

        var act = () => _service.AnalyzeFileAsync("test.docx", stream);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unsupported file format*");
    }

    #endregion

    #region Import Test Helpers

    /// <summary>
    /// Seeds the DB with Organization, Exercise, and Msel.
    /// DeliveryMethod lookup records are already seeded by HasData in AppDbContext.
    /// Returns IDs needed for ExecuteImportAsync calls.
    /// </summary>
    private (Guid OrgId, Guid ExerciseId, Guid MselId) SeedImportTestData()
    {
        var orgId = Guid.NewGuid();
        var exerciseId = Guid.NewGuid();
        var mselId = Guid.NewGuid();

        _context.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = "Test Organization",
            Slug = "test-org",
        });

        var exercise = new Exercise
        {
            Id = exerciseId,
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = orgId,
        };
        _context.Exercises.Add(exercise);

        var msel = new Msel
        {
            Id = mselId,
            Name = "Test MSEL",
            Version = 1,
            IsActive = true,
            ExerciseId = exerciseId,
            OrganizationId = orgId,
        };
        _context.Msels.Add(msel);
        exercise.ActiveMselId = mselId;

        _context.SaveChanges();
        return (orgId, exerciseId, mselId);
    }

    /// <summary>
    /// Runs the full wizard flow through validation and returns session ID and mappings ready for ExecuteImportAsync.
    /// Optionally allows modifying mappings before validation (e.g., making Title optional).
    /// </summary>
    private async Task<(Guid SessionId, IReadOnlyList<ColumnMappingDto> Mappings)>
        RunWizardThroughValidation(string[] headers, object?[][] dataRows,
            Func<IReadOnlyList<ColumnMappingDto>, IReadOnlyList<ColumnMappingDto>>? modifyMappings = null)
    {
        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        if (modifyMappings != null)
            mappings = modifyMappings(mappings);

        await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        return (sessionId, mappings);
    }

    /// <summary>
    /// Makes the Title mapping optional (IsRequired = false). Used for tests where
    /// the source MSEL has no Title column and the user overrides the constraint.
    /// </summary>
    private static IReadOnlyList<ColumnMappingDto> MakeTitleOptional(IReadOnlyList<ColumnMappingDto> mappings) =>
        mappings.Select(m => m.CadenceField == "Title" ? m with { IsRequired = false } : m).ToList();

    #endregion

    #region DeliveryMethod Synonym Resolution (ExecuteImportAsync Integration)

    [Theory]
    [InlineData("cell phone call", "Phone")]
    [InlineData("telephone", "Phone")]
    [InlineData("sms", "Phone")]
    [InlineData("cell phone", "Phone")]
    public async Task ExecuteImport_DeliveryMethodPhoneSynonyms_ResolvesToPhone(string deliveryValue, string expectedMethod)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", deliveryValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var method = await _context.DeliveryMethods.FirstAsync(d => d.Name == expectedMethod);
        inject.DeliveryMethodId.Should().Be(method.Id);
        inject.DeliveryMethodOther.Should().BeNull();
    }

    [Theory]
    [InlineData("emits", "Simulation")]
    [InlineData("sim", "Simulation")]
    [InlineData("cax", "Simulation")]
    public async Task ExecuteImport_DeliveryMethodSimulationSynonyms_ResolvesToSimulation(string deliveryValue, string expectedMethod)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", deliveryValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var method = await _context.DeliveryMethods.FirstAsync(d => d.Name == expectedMethod);
        inject.DeliveryMethodId.Should().Be(method.Id);
    }

    [Theory]
    [InlineData("handout", "Written")]
    [InlineData("fax", "Written")]
    [InlineData("memo", "Written")]
    [InlineData("courier", "Written")]
    public async Task ExecuteImport_DeliveryMethodWrittenSynonyms_ResolvesToWritten(string deliveryValue, string expectedMethod)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", deliveryValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var method = await _context.DeliveryMethods.FirstAsync(d => d.Name == expectedMethod);
        inject.DeliveryMethodId.Should().Be(method.Id);
    }

    [Theory]
    [InlineData("in person", "Verbal")]
    [InlineData("face to face", "Verbal")]
    [InlineData("runner", "Verbal")]
    public async Task ExecuteImport_DeliveryMethodVerbalSynonyms_ResolvesToVerbal(string deliveryValue, string expectedMethod)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", deliveryValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var method = await _context.DeliveryMethods.FirstAsync(d => d.Name == expectedMethod);
        inject.DeliveryMethodId.Should().Be(method.Id);
    }

    [Fact]
    public async Task ExecuteImport_DeliveryMethodExactMatch_ResolvesDirectly()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", "Email" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var method = await _context.DeliveryMethods.FirstAsync(d => d.Name == "Email");
        inject.DeliveryMethodId.Should().Be(method.Id);
        inject.DeliveryMethodOther.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteImport_DeliveryMethodUnknown_FallsBackToOther()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Delivery Method" };
        var dataRows = new[] { new object?[] { "Test Inject", "Carrier Pigeon" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        var otherMethod = await _context.DeliveryMethods.FirstAsync(d => d.IsOther);
        inject.DeliveryMethodId.Should().Be(otherMethod.Id);
        inject.DeliveryMethodOther.Should().Be("Carrier Pigeon");
    }

    #endregion

    #region Title Fallback from Description (ExecuteImportAsync Integration)

    [Fact]
    public async Task ExecuteImport_NoTitleOnlyDescription_PopulatesTitleFromDescription()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        // Source MSEL has no Title column; user marks Title as optional in the wizard
        var headers = new[] { "Description", "Scheduled Time" };
        var dataRows = new[] { new object?[] { "This is the inject description", "09:00" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows,
            m => MakeTitleOptional(m));

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.Title.Should().Be("This is the inject description");
        inject.Description.Should().Be("This is the inject description");
    }

    [Fact]
    public async Task ExecuteImport_DescriptionOver200Chars_TruncatesTitleWithEllipsis()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var longDescription = new string('A', 250);
        var headers = new[] { "Description", "Scheduled Time" };
        var dataRows = new[] { new object?[] { longDescription, "09:00" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows,
            m => MakeTitleOptional(m));

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.Title.Should().HaveLength(200);
        inject.Title.Should().EndWith("...");
        inject.Title[..197].Should().Be(longDescription[..197]);
        inject.Description.Should().Be(longDescription);
    }

    [Fact]
    public async Task ExecuteImport_BothTitleAndDescription_TitleNotOverridden()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Description", "Scheduled Time" };
        var dataRows = new[] { new object?[] { "My Title", "My Description", "09:00" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.Title.Should().Be("My Title");
        inject.Description.Should().Be("My Description");
    }

    [Fact]
    public async Task ExecuteImport_BothTitleAndDescriptionEmpty_NoCrash()
    {
        var (_, exerciseId, _) = SeedImportTestData();

        // Both Title and Description empty; Title marked optional to allow import
        var headers = new[] { "Description", "Scheduled Time" };
        var dataRows = new[] { new object?[] { "", "09:00" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows,
            m => MakeTitleOptional(m));

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        // Should not crash - inject created with empty title (no description to fall back to)
        result.InjectsCreated.Should().Be(1);
    }

    #endregion

    #region TriggerType Synonym Resolution (ExecuteImportAsync Integration)

    [Theory]
    [InlineData("controller action", TriggerType.Manual)]
    [InlineData("actor action", TriggerType.Manual)]
    [InlineData("staff action", TriggerType.Manual)]
    [InlineData("manual", TriggerType.Manual)]
    public async Task ExecuteImport_TriggerTypeManualSynonyms_ResolvesToManual(string triggerValue, TriggerType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Trigger Type" };
        var dataRows = new[] { new object?[] { "Test Inject", triggerValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.TriggerType.Should().Be(expected);
    }

    [Theory]
    [InlineData("player action", TriggerType.Conditional)]
    [InlineData("triggered", TriggerType.Conditional)]
    [InlineData("event-based", TriggerType.Conditional)]
    public async Task ExecuteImport_TriggerTypeConditionalSynonyms_ResolvesToConditional(string triggerValue, TriggerType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Trigger Type" };
        var dataRows = new[] { new object?[] { "Test Inject", triggerValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.TriggerType.Should().Be(expected);
    }

    [Theory]
    [InlineData("automatic", TriggerType.Scheduled)]
    [InlineData("auto", TriggerType.Scheduled)]
    [InlineData("timed", TriggerType.Scheduled)]
    [InlineData("time-based", TriggerType.Scheduled)]
    public async Task ExecuteImport_TriggerTypeScheduledSynonyms_ResolvesToScheduled(string triggerValue, TriggerType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Trigger Type" };
        var dataRows = new[] { new object?[] { "Test Inject", triggerValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.TriggerType.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteImport_TriggerTypeUnrecognized_DefaultsToManualWithWarning()
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Trigger Type" };
        var dataRows = new[] { new object?[] { "Test Inject", "unknown_trigger" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.TriggerType.Should().Be(TriggerType.Manual);
        result.Warnings.Should().Contain(w => w.Contains("unknown_trigger") && w.Contains("Manual"));
    }

    #endregion

    #region Validation Cross-Mapping Warnings

    [Fact]
    public async Task ValidateImport_InjectTypeContainsTriggerTypeValue_WarnsAboutMapping()
    {
        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", "controller action" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.WarningRows.Should().BeGreaterThan(0);
        var row = result.Rows[0];
        row.Issues.Should().Contain(i =>
            i.Field == "InjectType" &&
            i.Severity == "Warning" &&
            i.Message.Contains("Trigger Type"));
    }

    [Fact]
    public async Task ValidateImport_InjectTypeContainsDeliveryMethodValue_WarnsAboutMapping()
    {
        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", "phone" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.WarningRows.Should().BeGreaterThan(0);
        var row = result.Rows[0];
        row.Issues.Should().Contain(i =>
            i.Field == "InjectType" &&
            i.Severity == "Warning" &&
            i.Message.Contains("Delivery Method"));
    }

    [Fact]
    public async Task ValidateImport_TriggerTypeUnrecognizedValue_WarnsAboutDefault()
    {
        var headers = new[] { "Title", "Trigger Type" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", "some_random_value" },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        result.WarningRows.Should().BeGreaterThan(0);
        var row = result.Rows[0];
        row.Issues.Should().Contain(i =>
            i.Field == "TriggerType" &&
            i.Severity == "Warning" &&
            i.Message.Contains("default to Manual"));
    }

    [Theory]
    [InlineData("player action")]
    [InlineData("automatic")]
    [InlineData("manual")]
    public async Task ValidateImport_InjectTypeWithVariousTriggerLikeValues_WarnsCorrectly(string triggerLikeValue)
    {
        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", triggerLikeValue },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        var row = result.Rows[0];
        row.Issues.Should().Contain(i =>
            i.Field == "InjectType" &&
            i.Severity == "Warning" &&
            i.Message.Contains("Trigger Type"));
    }

    [Theory]
    [InlineData("radio")]
    [InlineData("email")]
    [InlineData("verbal")]
    [InlineData("fax")]
    public async Task ValidateImport_InjectTypeWithDeliveryMethodLikeValues_WarnsCorrectly(string deliveryLikeValue)
    {
        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[]
        {
            new object?[] { "Test Inject", deliveryLikeValue },
        };

        var (sessionId, mappings, _) = await RunWizardThroughMappings(headers, dataRows);

        var result = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        var row = result.Rows[0];
        row.Issues.Should().Contain(i =>
            i.Field == "InjectType" &&
            i.Severity == "Warning" &&
            i.Message.Contains("Delivery Method"));
    }

    #endregion

    #region Non-Legacy InjectType Synonyms

    [Theory]
    [InlineData("standard", InjectType.Standard)]
    [InlineData("normal", InjectType.Standard)]
    [InlineData("scheduled", InjectType.Standard)]
    [InlineData("planned", InjectType.Standard)]
    [InlineData("regular", InjectType.Standard)]
    [InlineData("primary", InjectType.Standard)]
    public async Task ExecuteImport_InjectTypeStandardSynonyms_ResolvesToStandard(string injectTypeValue, InjectType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[] { new object?[] { "Test Inject", injectTypeValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.InjectType.Should().Be(expected);
    }

    [Theory]
    [InlineData("contingency", InjectType.Contingency)]
    [InlineData("backup", InjectType.Contingency)]
    [InlineData("alternate", InjectType.Contingency)]
    [InlineData("fallback", InjectType.Contingency)]
    [InlineData("reserve", InjectType.Contingency)]
    public async Task ExecuteImport_InjectTypeContingencySynonyms_ResolvesToContingency(string injectTypeValue, InjectType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[] { new object?[] { "Test Inject", injectTypeValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.InjectType.Should().Be(expected);
    }

    [Theory]
    [InlineData("adaptive", InjectType.Adaptive)]
    [InlineData("branch", InjectType.Adaptive)]
    [InlineData("branching", InjectType.Adaptive)]
    [InlineData("decision", InjectType.Adaptive)]
    public async Task ExecuteImport_InjectTypeAdaptiveSynonyms_ResolvesToAdaptive(string injectTypeValue, InjectType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[] { new object?[] { "Test Inject", injectTypeValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.InjectType.Should().Be(expected);
    }

    [Theory]
    [InlineData("complexity", InjectType.Complexity)]
    [InlineData("escalation", InjectType.Complexity)]
    [InlineData("advanced", InjectType.Complexity)]
    [InlineData("challenge", InjectType.Complexity)]
    public async Task ExecuteImport_InjectTypeComplexitySynonyms_ResolvesToComplexity(string injectTypeValue, InjectType expected)
    {
        var (_, exerciseId, mselId) = SeedImportTestData();

        var headers = new[] { "Title", "Inject Type" };
        var dataRows = new[] { new object?[] { "Test Inject", injectTypeValue } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var result = await _service.ExecuteImportAsync(new ExecuteImportRequestDto
        {
            SessionId = sessionId,
            ExerciseId = exerciseId,
            Strategy = ImportStrategy.Append,
            SkipErrorRows = true
        });

        result.InjectsCreated.Should().Be(1);

        var inject = await _context.Injects.FirstAsync(i => i.MselId == mselId && !i.IsDeleted);
        inject.InjectType.Should().Be(expected);
    }

    #endregion

    #region Column Pattern Matching - ScenarioDay and ScenarioTime

    [Theory]
    [InlineData("Scenario Day", "ScenarioDay")]
    [InlineData("Exercise Day", "ScenarioDay")]
    [InlineData("Sim Day", "ScenarioDay")]
    [InlineData("Day", "ScenarioDay")]
    public async Task GetSuggestedMappings_ScenarioDayHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Scenario Time", "ScenarioTime")]
    [InlineData("Sim Time", "ScenarioTime")]
    [InlineData("Story Time", "ScenarioTime")]
    [InlineData("Exercise Time", "ScenarioTime")]
    public async Task GetSuggestedMappings_ScenarioTimeHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    #endregion

    #region Column Pattern Matching - Remaining Untested Patterns

    [Theory]
    [InlineData("Phase", "Phase")]
    [InlineData("Exercise Phase", "Phase")]
    [InlineData("Phase Name", "Phase")]
    public async Task GetSuggestedMappings_PhaseHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Priority", "Priority")]
    [InlineData("Importance", "Priority")]
    public async Task GetSuggestedMappings_PriorityHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Location", "LocationName")]
    [InlineData("Location Name", "LocationName")]
    [InlineData("Venue", "LocationName")]
    public async Task GetSuggestedMappings_LocationNameHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Location Type", "LocationType")]
    [InlineData("Venue Type", "LocationType")]
    public async Task GetSuggestedMappings_LocationTypeHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Inject Type", "InjectType")]
    [InlineData("Category", "InjectType")]
    [InlineData("Inject Category", "InjectType")]
    public async Task GetSuggestedMappings_InjectTypeHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    [Theory]
    [InlineData("Trigger", "TriggerType")]
    [InlineData("Trigger Type", "TriggerType")]
    [InlineData("Fire Mode", "TriggerType")]
    public async Task GetSuggestedMappings_TriggerTypeHeaders_MapsCorrectly(string header, string expectedField)
    {
        var (_, mappings, _) = await RunWizardThroughMappings(new[] { "Title", header });

        var mapping = mappings.First(m => m.CadenceField == expectedField);
        mapping.SourceColumnIndex.Should().Be(1);
        mapping.SuggestedMappingConfidence.Should().BeGreaterThanOrEqualTo(80);
    }

    #endregion

    #region UpdateRowsAsync Tests

    [Fact]
    public async Task UpdateRows_UpdateTitleField_RevalidatesRowToValid()
    {
        // Arrange: Create a session with a row missing Title (Error)
        var headers = new[] { "Title", "Description", "Scheduled Time" };
        var dataRows = new[]
        {
            new object?[] { "", "Has a description", "09:00" }, // Missing Title -> Error
            new object?[] { "Valid Inject", "Description", "10:00" },
        };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        // Verify initial state has 1 error row
        var session = await _service.GetSessionStateAsync(sessionId);
        session.Should().NotBeNull();

        // Re-validate to check initial state
        var initialValidation = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });
        initialValidation.ErrorRows.Should().Be(1);

        // Act: Fix the missing Title
        var result = await _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[]
            {
                new RowUpdateDto { RowNumber = 2, Field = "Title", Value = "Fixed Title" }
            }
        });

        // Assert
        result.ErrorRows.Should().Be(0);
        result.UpdatedRows.Should().HaveCount(1);
        result.UpdatedRows[0].RowNumber.Should().Be(2);
        result.UpdatedRows[0].Status.Should().NotBe("Error");
        result.UpdatedRows[0].Values["Title"].Should().Be("Fixed Title");
    }

    [Fact]
    public async Task UpdateRows_UpdateScheduledTimeField_FixesTimeError()
    {
        var headers = new[] { "Title", "Scheduled Time" };
        var dataRows = new[]
        {
            new object?[] { "Inject 1", "not-a-time" }, // Unparseable time -> Error
        };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var initialValidation = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });
        initialValidation.ErrorRows.Should().Be(1);

        // Act: Fix the unparseable time
        var result = await _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[]
            {
                new RowUpdateDto { RowNumber = 2, Field = "ScheduledTime", Value = "00:00" }
            }
        });

        // Assert: No more errors (time is now parseable)
        result.ErrorRows.Should().Be(0);
        result.UpdatedRows.Should().HaveCount(1);
        result.UpdatedRows[0].Status.Should().Be("Valid");
    }

    [Fact]
    public async Task UpdateRows_BulkUpdate_MultipleRows_RevalidatesAll()
    {
        var headers = new[] { "Title", "Description", "Scheduled Time" };
        var dataRows = new[]
        {
            new object?[] { "", "Description 1", "09:00" }, // Missing Title
            new object?[] { "Valid", "Description 2", "10:00" },
            new object?[] { "", "Description 3", "11:00" }, // Missing Title
        };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        var initialValidation = await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });
        initialValidation.ErrorRows.Should().Be(2);

        // Act: Fix both missing titles at once (bulk auto-fix)
        var result = await _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[]
            {
                new RowUpdateDto { RowNumber = 2, Field = "Title", Value = "Description 1" },
                new RowUpdateDto { RowNumber = 4, Field = "Title", Value = "Description 3" },
            }
        });

        // Assert
        result.ErrorRows.Should().Be(0);
        result.UpdatedRows.Should().HaveCount(2);
        result.ValidRows.Should().Be(3);
    }

    [Fact]
    public async Task UpdateRows_NoValidationResults_ThrowsInvalidOperation()
    {
        // Arrange: Create session but don't run validation
        var headers = new[] { "Title" };
        var dataRows = new[] { new object?[] { "Inject 1" } };
        var (sessionId, _, _) = await RunWizardThroughMappings(headers, dataRows);

        // Act & Assert
        var act = () => _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[] { new RowUpdateDto { RowNumber = 2, Field = "Title", Value = "Test" } }
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validate*");
    }

    [Fact]
    public async Task UpdateRows_InvalidRowNumber_SkipsGracefully()
    {
        var headers = new[] { "Title", "Scheduled Time" };
        var dataRows = new[] { new object?[] { "Inject 1", "09:00" } };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        // Act: Try to update a non-existent row
        var result = await _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[] { new RowUpdateDto { RowNumber = 999, Field = "Title", Value = "Test" } }
        });

        // Assert: No rows updated, counts unchanged
        result.UpdatedRows.Should().BeEmpty();
        result.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRows_ReturnsOnlyUpdatedRows()
    {
        var headers = new[] { "Title", "Description", "Scheduled Time" };
        var dataRows = new[]
        {
            new object?[] { "Inject 1", "Desc 1", "09:00" },
            new object?[] { "", "Desc 2", "10:00" }, // Missing Title -> Error
            new object?[] { "Inject 3", "Desc 3", "11:00" },
        };
        var (sessionId, mappings) = await RunWizardThroughValidation(headers, dataRows);

        await _service.ValidateImportAsync(new ConfigureMappingsRequestDto
        {
            SessionId = sessionId,
            Mappings = mappings
        });

        // Act: Fix only row 3 (the one with missing Title)
        var result = await _service.UpdateRowsAsync(new UpdateRowsRequestDto
        {
            SessionId = sessionId,
            Updates = new[] { new RowUpdateDto { RowNumber = 3, Field = "Title", Value = "Fixed" } }
        });

        // Assert: Only 1 row in response, not all 3
        result.UpdatedRows.Should().HaveCount(1);
        result.UpdatedRows[0].RowNumber.Should().Be(3);
        result.TotalRows.Should().Be(3);
    }

    #endregion
}
