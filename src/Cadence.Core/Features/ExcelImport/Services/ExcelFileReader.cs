using System.Data;
using Cadence.Core.Features.ExcelImport.Models;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using ClosedXML.Excel;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Stateless helpers for reading raw data from CSV, XLS, and XLSX files into
/// format-agnostic row dictionaries consumed by the import pipeline.
/// </summary>
internal static class ExcelFileReader
{
    // Maximum rows and columns to process (guards against pathological files).
    private const int MaxRows = 5000;
    private const int MaxColumns = 100;

    // -----------------------------------------------------------------------
    // Worksheet analysis (header-row detection)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Scans the first few rows of an XLSX worksheet to find the most likely
    /// header row and assess whether the sheet looks like a MSEL.
    /// </summary>
    /// <returns>
    /// A tuple of (LooksLikeMsel, Confidence 0-100, 1-based SuggestedHeaderRow).
    /// </returns>
    public static (bool LooksLikeMsel, int Confidence, int SuggestedHeaderRow) AnalyzeWorksheetHeaders(
        IXLWorksheet worksheet)
    {
        var bestRow = 1;
        var bestConfidence = 0;
        var bestMatches = 0;

        var maxScanRow = Math.Min(10, worksheet.LastRowUsed()?.RowNumber() ?? 1);
        var lastCol = Math.Min(worksheet.LastColumnUsed()?.ColumnNumber() ?? 0, MaxColumns);

        var keyPatterns = new[] { "title", "subject", "time", "dtg", "inject", "description", "text", "from", "to", "msel" };

        for (int row = 1; row <= maxScanRow; row++)
        {
            var headers = new List<string>();
            for (int col = 1; col <= lastCol; col++)
            {
                headers.Add(worksheet.Row(row).Cell(col).GetString().Trim().ToLowerInvariant());
            }

            var matchedPatterns = 0;
            foreach (var pattern in keyPatterns)
            {
                if (headers.Any(h => h.Contains(pattern)))
                {
                    matchedPatterns++;
                }
            }

            if (matchedPatterns > bestMatches)
            {
                bestMatches = matchedPatterns;
                bestRow = row;
                bestConfidence = (matchedPatterns * 100) / keyPatterns.Length;
            }
        }

        return (bestMatches >= 2, Math.Min(100, bestConfidence + 20), bestRow);
    }

    /// <summary>
    /// Scans the first few rows of a DataTable (from a legacy .xls file) to find the
    /// most likely header row and assess whether the sheet looks like a MSEL.
    /// Returns a 1-based header row number for consistency with the rest of the code.
    /// </summary>
    public static (bool LooksLikeMsel, int Confidence, int SuggestedHeaderRow) AnalyzeDataTableHeaders(
        DataTable table)
    {
        var bestRow = 1;
        var bestConfidence = 0;
        var bestMatches = 0;

        var maxScanRow = Math.Min(10, table.Rows.Count);
        var lastCol = Math.Min(LegacyExcelReader.GetLastUsedColumn(table) + 1, MaxColumns);

        var keyPatterns = new[] { "title", "subject", "time", "dtg", "inject", "description", "text", "from", "to", "msel" };

        for (int row = 0; row < maxScanRow; row++)
        {
            var headers = new List<string>();
            var dataRow = table.Rows[row];
            for (int col = 0; col < lastCol; col++)
            {
                headers.Add(LegacyExcelReader.GetCellString(dataRow, col).ToLowerInvariant());
            }

            var matchedPatterns = 0;
            foreach (var pattern in keyPatterns)
            {
                if (headers.Any(h => h.Contains(pattern)))
                {
                    matchedPatterns++;
                }
            }

            if (matchedPatterns > bestMatches)
            {
                bestMatches = matchedPatterns;
                bestRow = row + 1; // Convert to 1-based
                bestConfidence = (matchedPatterns * 100) / keyPatterns.Length;
            }
        }

        return (bestMatches >= 2, Math.Min(100, bestConfidence + 20), bestRow);
    }

    // -----------------------------------------------------------------------
    // Column metadata
    // -----------------------------------------------------------------------

    /// <summary>
    /// Samples up to 100 rows of an XLSX column starting at <paramref name="startRow"/>
    /// to infer the data type, collect representative values, and compute a fill rate.
    /// </summary>
    public static (string DataType, IReadOnlyList<string?> SampleValues, int FillRate) GetColumnData(
        IXLWorksheet worksheet, int column, int startRow)
    {
        var samples = new List<string?>();
        var types = new List<string>();
        var filledCount = 0;
        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? startRow, startRow + 100);

        for (int row = startRow; row <= lastRow; row++)
        {
            var cell = worksheet.Cell(row, column);
            if (!cell.IsEmpty())
            {
                filledCount++;
                if (samples.Count < 3)
                {
                    samples.Add(cell.GetString());
                }
                types.Add(InferCellType(cell));
            }
        }

        var totalRows = lastRow - startRow + 1;
        var fillRate = totalRows > 0 ? (filledCount * 100) / totalRows : 0;

        var dataType = types.Count > 0
            ? types.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key
            : "text";

        return (dataType, samples, fillRate);
    }

    /// <summary>
    /// Samples up to 100 rows of a DataTable column starting at <paramref name="startRow"/> (0-based)
    /// to infer the data type, collect representative values, and compute a fill rate.
    /// </summary>
    public static (string DataType, IReadOnlyList<string?> SampleValues, int FillRate) GetDataTableColumnData(
        DataTable table, int column, int startRow)
    {
        var samples = new List<string?>();
        var types = new List<string>();
        var filledCount = 0;
        var lastRow = Math.Min(table.Rows.Count - 1, startRow + 100);

        for (int row = startRow; row <= lastRow; row++)
        {
            var dataRow = table.Rows[row];
            if (!LegacyExcelReader.IsCellEmpty(dataRow, column))
            {
                filledCount++;
                if (samples.Count < 3)
                {
                    samples.Add(LegacyExcelReader.GetCellValue(dataRow, column)?.ToString());
                }
                types.Add(LegacyExcelReader.InferCellType(LegacyExcelReader.GetCellValue(dataRow, column)));
            }
        }

        var totalRows = lastRow - startRow + 1;
        var fillRate = totalRows > 0 ? (filledCount * 100) / totalRows : 0;

        var dataType = types.Count > 0
            ? types.GroupBy(t => t).OrderByDescending(g => g.Count()).First().Key
            : "text";

        return (dataType, samples, fillRate);
    }

    // -----------------------------------------------------------------------
    // Row reading
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reads all data rows from the file referenced by <paramref name="session"/>
    /// and returns them as a list of field-name → value dictionaries keyed by
    /// the Cadence field name from each mapping.
    /// </summary>
    public static async Task<List<Dictionary<string, object?>>> ReadAllRowsAsync(
        ImportSession session,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        return session.FileFormat switch
        {
            "csv" => await ReadCsvRowsAsync(session, mappings),
            "xls" => await ReadXlsRowsAsync(session, mappings),
            _ => await ReadXlsxRowsAsync(session, mappings)
        };
    }

    // -----------------------------------------------------------------------
    // CSV helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reads all text lines from a CSV stream.
    /// </summary>
    public static async Task<List<string>> ReadCsvLinesAsync(Stream stream)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// Parses a single CSV line into a list of field values, respecting
    /// double-quoted fields that may contain commas.
    /// </summary>
    public static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }
        values.Add(current.Trim());

        return values;
    }

    // -----------------------------------------------------------------------
    // XLSX helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the typed value of an XLSX cell, or <c>null</c> for empty cells.
    /// </summary>
    public static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        return cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.Number   => cell.GetDouble(),
            XLDataType.Boolean  => cell.GetBoolean(),
            XLDataType.TimeSpan => cell.GetTimeSpan().ToString(),
            _                   => cell.GetString()
        };
    }

    /// <summary>
    /// Returns a simple type name string for an XLSX cell.
    /// </summary>
    public static string InferCellType(IXLCell cell)
    {
        if (cell.IsEmpty()) return "empty";
        if (cell.DataType == XLDataType.DateTime) return "date";
        if (cell.DataType == XLDataType.Number) return "number";
        if (cell.DataType == XLDataType.Boolean) return "boolean";
        if (cell.DataType == XLDataType.TimeSpan) return "time";
        return "text";
    }

    // -----------------------------------------------------------------------
    // Column letter helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Converts a 1-based column number to its spreadsheet letter notation
    /// (e.g., 1 → "A", 27 → "AA").
    /// </summary>
    public static string GetColumnLetter(int columnNumber)
    {
        var result = "";
        while (columnNumber > 0)
        {
            columnNumber--;
            result = (char)('A' + columnNumber % 26) + result;
            columnNumber /= 26;
        }
        return result;
    }

    // -----------------------------------------------------------------------
    // Private format-specific row readers
    // -----------------------------------------------------------------------

    private static async Task<List<Dictionary<string, object?>>> ReadCsvRowsAsync(
        ImportSession session,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        var rows = new List<Dictionary<string, object?>>();
        var lines = await File.ReadAllLinesAsync(session.TempFilePath);

        for (int i = session.DataStartRow - 1; i < lines.Length && i < MaxRows; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var rowData = new Dictionary<string, object?>();

            foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
            {
                var colIndex = mapping.SourceColumnIndex!.Value;
                rowData[mapping.CadenceField] = colIndex < values.Count ? values[colIndex] : null;
            }

            if (rowData.Values.All(RowValidationService.IsEmpty))
                continue;

            rows.Add(rowData);
        }

        return rows;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadXlsRowsAsync(
        ImportSession session,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        if (!session.SelectedWorksheetIndex.HasValue)
        {
            throw new InvalidOperationException("Worksheet index not set for Excel file import");
        }

        var rows = new List<Dictionary<string, object?>>();

        await using var fileStream = File.OpenRead(session.TempFilePath);
        var dataSet = LegacyExcelReader.ReadToDataSet(fileStream);
        var table = dataSet.Tables[session.SelectedWorksheetIndex.Value];

        // DataTable rows are 0-based; session.DataStartRow is 1-based
        var startRowIndex = session.DataStartRow - 1;
        var lastRowIndex = Math.Min(LegacyExcelReader.GetLastUsedRow(table), startRowIndex + MaxRows - 1);

        for (int row = startRowIndex; row <= lastRowIndex; row++)
        {
            var dataRow = table.Rows[row];
            var rowData = new Dictionary<string, object?>();

            foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
            {
                rowData[mapping.CadenceField] = LegacyExcelReader.GetCellValue(dataRow, mapping.SourceColumnIndex!.Value);
            }

            if (rowData.Values.All(RowValidationService.IsEmpty))
                continue;

            rows.Add(rowData);
        }

        return rows;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadXlsxRowsAsync(
        ImportSession session,
        IReadOnlyList<ColumnMappingDto> mappings)
    {
        if (!session.SelectedWorksheetIndex.HasValue)
        {
            throw new InvalidOperationException("Worksheet index not set for Excel file import");
        }

        var rows = new List<Dictionary<string, object?>>();

        await using var fileStream = File.OpenRead(session.TempFilePath);
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(session.SelectedWorksheetIndex.Value + 1);

        var lastRow = Math.Min(
            worksheet.LastRowUsed()?.RowNumber() ?? 0,
            session.DataStartRow + MaxRows - 1);

        for (int row = session.DataStartRow; row <= lastRow; row++)
        {
            var rowData = new Dictionary<string, object?>();

            foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
            {
                var cell = worksheet.Cell(row, mapping.SourceColumnIndex!.Value + 1);
                rowData[mapping.CadenceField] = GetCellValue(cell);
            }

            if (rowData.Values.All(RowValidationService.IsEmpty))
                continue;

            rows.Add(rowData);
        }

        return rows;
    }
}
