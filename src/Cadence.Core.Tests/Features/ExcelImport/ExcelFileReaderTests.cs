using System.Data;
using System.Text;
using Cadence.Core.Features.ExcelImport.Services;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

/// <summary>
/// Tests for ExcelFileReader — header analysis, CSV parsing, cell type inference,
/// column metadata, and column letter conversion.
/// </summary>
public class ExcelFileReaderTests
{
    // =========================================================================
    // ParseCsvLine
    // =========================================================================

    [Fact]
    public void ParseCsvLine_SimpleValues_SplitsCorrectly()
    {
        var result = ExcelFileReader.ParseCsvLine("A,B,C");

        result.Should().Equal("A", "B", "C");
    }

    [Fact]
    public void ParseCsvLine_QuotedComma_PreservesField()
    {
        var result = ExcelFileReader.ParseCsvLine("\"Hello, World\",B,C");

        result[0].Should().Be("Hello, World");
        result.Should().HaveCount(3);
    }

    [Fact]
    public void ParseCsvLine_EmptyFields_PreservesEmpties()
    {
        var result = ExcelFileReader.ParseCsvLine("A,,C,");

        result.Should().Equal("A", "", "C", "");
    }

    [Fact]
    public void ParseCsvLine_TrimsWhitespace()
    {
        var result = ExcelFileReader.ParseCsvLine(" A , B , C ");

        result.Should().Equal("A", "B", "C");
    }

    [Fact]
    public void ParseCsvLine_EmptyLine_ReturnsSingleEmptyField()
    {
        var result = ExcelFileReader.ParseCsvLine("");

        result.Should().Equal("");
    }

    // =========================================================================
    // ReadCsvLinesAsync
    // =========================================================================

    [Fact]
    public async Task ReadCsvLinesAsync_MultipleLines_ReadsAll()
    {
        var csv = "Line 1\nLine 2\nLine 3";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await ExcelFileReader.ReadCsvLinesAsync(stream);

        result.Should().HaveCount(3);
        result[0].Should().Be("Line 1");
    }

    [Fact]
    public async Task ReadCsvLinesAsync_EmptyStream_ReturnsEmpty()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = await ExcelFileReader.ReadCsvLinesAsync(stream);

        result.Should().BeEmpty();
    }

    // =========================================================================
    // GetColumnLetter
    // =========================================================================

    [Theory]
    [InlineData(1, "A")]
    [InlineData(26, "Z")]
    [InlineData(27, "AA")]
    [InlineData(52, "AZ")]
    [InlineData(53, "BA")]
    [InlineData(702, "ZZ")]
    public void GetColumnLetter_ConvertsCorrectly(int column, string expected)
    {
        ExcelFileReader.GetColumnLetter(column).Should().Be(expected);
    }

    // =========================================================================
    // AnalyzeWorksheetHeaders (XLSX)
    // =========================================================================

    [Fact]
    public void AnalyzeWorksheetHeaders_MselHeaders_DetectsAsMsel()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Inject Number";
        ws.Cell(1, 2).Value = "Title";
        ws.Cell(1, 3).Value = "Description";
        ws.Cell(1, 4).Value = "Time";

        var (looksLikeMsel, confidence, headerRow) = ExcelFileReader.AnalyzeWorksheetHeaders(ws);

        looksLikeMsel.Should().BeTrue();
        confidence.Should().BeGreaterThan(40);
        headerRow.Should().Be(1);
    }

    [Fact]
    public void AnalyzeWorksheetHeaders_HeaderOnRow3_FindsCorrectRow()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        // Rows 1-2 are title/blank
        ws.Cell(1, 1).Value = "Exercise Report";
        ws.Cell(3, 1).Value = "Inject";
        ws.Cell(3, 2).Value = "Title";
        ws.Cell(3, 3).Value = "Description";
        ws.Cell(3, 4).Value = "Time";
        ws.Cell(3, 5).Value = "From";

        var (_, _, headerRow) = ExcelFileReader.AnalyzeWorksheetHeaders(ws);

        headerRow.Should().Be(3);
    }

    [Fact]
    public void AnalyzeWorksheetHeaders_NoMselKeywords_LowConfidence()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "ID";
        ws.Cell(1, 2).Value = "Value";

        var (looksLikeMsel, confidence, _) = ExcelFileReader.AnalyzeWorksheetHeaders(ws);

        looksLikeMsel.Should().BeFalse();
        confidence.Should().BeLessThan(50);
    }

    // =========================================================================
    // AnalyzeDataTableHeaders (XLS DataTable)
    // =========================================================================

    [Fact]
    public void AnalyzeDataTableHeaders_MselHeaders_DetectsAsMsel()
    {
        var table = new DataTable();
        table.Columns.Add("C1", typeof(object));
        table.Columns.Add("C2", typeof(object));
        table.Columns.Add("C3", typeof(object));
        table.Columns.Add("C4", typeof(object));

        var row = table.NewRow();
        row[0] = "Inject";
        row[1] = "Title";
        row[2] = "Description";
        row[3] = "Time";
        table.Rows.Add(row);

        var (looksLikeMsel, confidence, headerRow) = ExcelFileReader.AnalyzeDataTableHeaders(table);

        looksLikeMsel.Should().BeTrue();
        confidence.Should().BeGreaterThan(40);
        headerRow.Should().Be(1); // 1-based
    }

    [Fact]
    public void AnalyzeDataTableHeaders_EmptyTable_ReturnsLowConfidence()
    {
        var table = new DataTable();
        table.Columns.Add("C1", typeof(object));

        var (looksLikeMsel, _, _) = ExcelFileReader.AnalyzeDataTableHeaders(table);

        looksLikeMsel.Should().BeFalse();
    }

    // =========================================================================
    // GetColumnData (XLSX)
    // =========================================================================

    [Fact]
    public void GetColumnData_NumericColumn_InfersNumberType()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Header";
        ws.Cell(2, 1).Value = 42;
        ws.Cell(3, 1).Value = 99.5;
        ws.Cell(4, 1).Value = 7;

        var (dataType, samples, fillRate) = ExcelFileReader.GetColumnData(ws, 1, 2);

        dataType.Should().Be("number");
        samples.Should().HaveCountGreaterOrEqualTo(3);
        fillRate.Should().Be(100);
    }

    [Fact]
    public void GetColumnData_EmptyColumn_ReturnsTextTypeZeroFillRate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "Header";
        // No data rows

        var (dataType, samples, fillRate) = ExcelFileReader.GetColumnData(ws, 1, 2);

        dataType.Should().Be("text");
        fillRate.Should().Be(0);
    }

    // =========================================================================
    // GetDataTableColumnData (DataTable)
    // =========================================================================

    [Fact]
    public void GetDataTableColumnData_MixedData_InfersMostCommonType()
    {
        var table = new DataTable();
        table.Columns.Add("C1", typeof(object));

        for (int i = 0; i < 5; i++)
        {
            var row = table.NewRow();
            row[0] = $"Text {i}";
            table.Rows.Add(row);
        }

        var (dataType, samples, fillRate) = ExcelFileReader.GetDataTableColumnData(table, 0, 0);

        dataType.Should().Be("text");
        samples.Should().HaveCount(3); // Max 3 samples
        fillRate.Should().Be(100);
    }

    // =========================================================================
    // InferCellType (XLSX cell)
    // =========================================================================

    [Fact]
    public void InferCellType_EmptyCell_ReturnsEmpty()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");

        ExcelFileReader.InferCellType(ws.Cell(1, 1)).Should().Be("empty");
    }

    [Fact]
    public void InferCellType_DateCell_ReturnsDate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = new DateTime(2026, 1, 1);

        ExcelFileReader.InferCellType(ws.Cell(1, 1)).Should().Be("date");
    }

    [Fact]
    public void InferCellType_NumberCell_ReturnsNumber()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = 42.5;

        ExcelFileReader.InferCellType(ws.Cell(1, 1)).Should().Be("number");
    }

    [Fact]
    public void InferCellType_BooleanCell_ReturnsBoolean()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = true;

        ExcelFileReader.InferCellType(ws.Cell(1, 1)).Should().Be("boolean");
    }

    [Fact]
    public void InferCellType_TextCell_ReturnsText()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "hello";

        ExcelFileReader.InferCellType(ws.Cell(1, 1)).Should().Be("text");
    }

    // =========================================================================
    // GetCellValue (XLSX cell)
    // =========================================================================

    [Fact]
    public void GetCellValue_EmptyCell_ReturnsNull()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");

        ExcelFileReader.GetCellValue(ws.Cell(1, 1)).Should().BeNull();
    }

    [Fact]
    public void GetCellValue_NumberCell_ReturnsDouble()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = 42.5;

        ExcelFileReader.GetCellValue(ws.Cell(1, 1)).Should().Be(42.5);
    }

    [Fact]
    public void GetCellValue_TextCell_ReturnsString()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = "hello";

        ExcelFileReader.GetCellValue(ws.Cell(1, 1)).Should().Be("hello");
    }

    [Fact]
    public void GetCellValue_BooleanCell_ReturnsBool()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = true;

        ExcelFileReader.GetCellValue(ws.Cell(1, 1)).Should().Be(true);
    }

    [Fact]
    public void GetCellValue_DateTimeCell_ReturnsDateTime()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Sheet1");
        ws.Cell(1, 1).Value = new DateTime(2026, 3, 15, 10, 30, 0);

        var result = ExcelFileReader.GetCellValue(ws.Cell(1, 1));

        result.Should().BeOfType<DateTime>();
        ((DateTime)result!).Hour.Should().Be(10);
        ((DateTime)result!).Minute.Should().Be(30);
    }
}
