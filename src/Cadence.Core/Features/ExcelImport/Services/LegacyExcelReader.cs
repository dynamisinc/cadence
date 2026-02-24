using System.Data;
using System.Text;
using ExcelDataReader;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Reads legacy .xls (Excel 97-2003 BIFF) files using ExcelDataReader.
/// ClosedXML only supports .xlsx; this class bridges the gap for .xls files.
/// </summary>
public static class LegacyExcelReader
{
    private static bool _encodingRegistered;
    private static readonly object _encodingLock = new();

    /// <summary>
    /// Ensures the CodePages encoding provider is registered (required for .xls on .NET Core+).
    /// </summary>
    public static void EnsureEncodingRegistered()
    {
        if (_encodingRegistered) return;
        lock (_encodingLock)
        {
            if (_encodingRegistered) return;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _encodingRegistered = true;
        }
    }

    /// <summary>
    /// Reads a .xls file stream into a DataSet with one DataTable per worksheet.
    /// All values are read as raw data (no header row assumption).
    /// </summary>
    public static DataSet ReadToDataSet(Stream stream)
    {
        EnsureEncodingRegistered();

        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
        {
            LeaveOpen = true
        });
        return reader.AsDataSet(new ExcelDataSetConfiguration
        {
            UseColumnDataType = false, // Read everything as object, handle types ourselves
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false // We manage header detection ourselves
            }
        });
    }

    /// <summary>
    /// Gets the string value of a cell from a DataRow, trimming whitespace.
    /// Returns empty string for null/DBNull values.
    /// </summary>
    public static string GetCellString(DataRow row, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= row.Table.Columns.Count)
            return string.Empty;

        var value = row[columnIndex];
        if (value == null || value == DBNull.Value)
            return string.Empty;

        return value.ToString()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets the raw cell value from a DataRow, returning null for DBNull.
    /// </summary>
    public static object? GetCellValue(DataRow row, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= row.Table.Columns.Count)
            return null;

        var value = row[columnIndex];
        if (value == null || value == DBNull.Value)
            return null;

        return value;
    }

    /// <summary>
    /// Checks if a cell is empty (null, DBNull, or whitespace string).
    /// </summary>
    public static bool IsCellEmpty(DataRow row, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= row.Table.Columns.Count)
            return true;

        var value = row[columnIndex];
        if (value == null || value == DBNull.Value)
            return true;

        return value is string s && string.IsNullOrWhiteSpace(s);
    }

    /// <summary>
    /// Infers a simple data type string from a cell value.
    /// </summary>
    public static string InferCellType(object? value)
    {
        if (value == null || value == DBNull.Value)
            return "empty";

        return value switch
        {
            DateTime => "date",
            double or float or decimal or int or long => "number",
            bool => "boolean",
            TimeSpan => "time",
            _ => "text"
        };
    }

    /// <summary>
    /// Finds the last used row index (0-based) in a DataTable.
    /// Scans from the bottom to find the last row with any non-empty cell.
    /// </summary>
    public static int GetLastUsedRow(DataTable table)
    {
        for (int row = table.Rows.Count - 1; row >= 0; row--)
        {
            for (int col = 0; col < table.Columns.Count; col++)
            {
                if (!IsCellEmpty(table.Rows[row], col))
                    return row;
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds the last used column index (0-based) in a DataTable.
    /// Scans columns to find the last one with any non-empty cell in the first N rows.
    /// </summary>
    public static int GetLastUsedColumn(DataTable table, int scanRows = 100)
    {
        var maxRow = Math.Min(table.Rows.Count, scanRows);
        var lastCol = -1;

        for (int row = 0; row < maxRow; row++)
        {
            for (int col = table.Columns.Count - 1; col > lastCol; col--)
            {
                if (!IsCellEmpty(table.Rows[row], col))
                {
                    lastCol = col;
                    break;
                }
            }
        }

        return lastCol;
    }
}
