using System.Data;
using Cadence.Core.Features.ExcelImport.Services;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

/// <summary>
/// Tests for LegacyExcelReader — static utility for reading .xls files
/// and manipulating DataTable cells.
/// </summary>
public class LegacyExcelReaderTests
{
    private static DataTable CreateTable(int columns, params object?[][] rows)
    {
        var table = new DataTable();
        for (int i = 0; i < columns; i++)
            table.Columns.Add($"Col{i}", typeof(object));

        foreach (var row in rows)
        {
            var dataRow = table.NewRow();
            for (int i = 0; i < row.Length && i < columns; i++)
                dataRow[i] = row[i] ?? DBNull.Value;
            table.Rows.Add(dataRow);
        }
        return table;
    }

    // =========================================================================
    // EnsureEncodingRegistered
    // =========================================================================

    [Fact]
    public void EnsureEncodingRegistered_CanBeCalledMultipleTimes()
    {
        // Idempotent — should not throw on repeated calls
        LegacyExcelReader.EnsureEncodingRegistered();
        LegacyExcelReader.EnsureEncodingRegistered();
    }

    // =========================================================================
    // GetCellString
    // =========================================================================

    [Fact]
    public void GetCellString_ValidString_ReturnsTrimmedValue()
    {
        var table = CreateTable(1, new object?[] { "  hello  " });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], 0);

        result.Should().Be("hello");
    }

    [Fact]
    public void GetCellString_NullValue_ReturnsEmptyString()
    {
        var table = CreateTable(1, new object?[] { null });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCellString_DBNullValue_ReturnsEmptyString()
    {
        var table = CreateTable(1, new object?[] { DBNull.Value });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], 0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCellString_OutOfBoundsColumn_ReturnsEmptyString()
    {
        var table = CreateTable(1, new object?[] { "data" });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], 5);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCellString_NegativeColumnIndex_ReturnsEmptyString()
    {
        var table = CreateTable(1, new object?[] { "data" });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], -1);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCellString_NumericValue_ReturnsStringRepresentation()
    {
        var table = CreateTable(1, new object?[] { 42.5 });

        var result = LegacyExcelReader.GetCellString(table.Rows[0], 0);

        result.Should().Be("42.5");
    }

    // =========================================================================
    // GetCellValue
    // =========================================================================

    [Fact]
    public void GetCellValue_ValidValue_ReturnsRawObject()
    {
        var table = CreateTable(1, new object?[] { 42.5 });

        var result = LegacyExcelReader.GetCellValue(table.Rows[0], 0);

        result.Should().Be(42.5);
    }

    [Fact]
    public void GetCellValue_NullValue_ReturnsNull()
    {
        var table = CreateTable(1, new object?[] { null });

        var result = LegacyExcelReader.GetCellValue(table.Rows[0], 0);

        result.Should().BeNull();
    }

    [Fact]
    public void GetCellValue_DBNullValue_ReturnsNull()
    {
        var table = CreateTable(1, new object?[] { DBNull.Value });

        var result = LegacyExcelReader.GetCellValue(table.Rows[0], 0);

        result.Should().BeNull();
    }

    [Fact]
    public void GetCellValue_OutOfBoundsColumn_ReturnsNull()
    {
        var table = CreateTable(1, new object?[] { "data" });

        var result = LegacyExcelReader.GetCellValue(table.Rows[0], 99);

        result.Should().BeNull();
    }

    [Fact]
    public void GetCellValue_NegativeColumnIndex_ReturnsNull()
    {
        var table = CreateTable(1, new object?[] { "data" });

        var result = LegacyExcelReader.GetCellValue(table.Rows[0], -1);

        result.Should().BeNull();
    }

    // =========================================================================
    // IsCellEmpty
    // =========================================================================

    [Fact]
    public void IsCellEmpty_NullValue_ReturnsTrue()
    {
        var table = CreateTable(1, new object?[] { null });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 0).Should().BeTrue();
    }

    [Fact]
    public void IsCellEmpty_DBNullValue_ReturnsTrue()
    {
        var table = CreateTable(1, new object?[] { DBNull.Value });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 0).Should().BeTrue();
    }

    [Fact]
    public void IsCellEmpty_WhitespaceString_ReturnsTrue()
    {
        var table = CreateTable(1, new object?[] { "   " });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 0).Should().BeTrue();
    }

    [Fact]
    public void IsCellEmpty_OutOfBoundsColumn_ReturnsTrue()
    {
        var table = CreateTable(1, new object?[] { "data" });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 10).Should().BeTrue();
    }

    [Fact]
    public void IsCellEmpty_NegativeColumnIndex_ReturnsTrue()
    {
        var table = CreateTable(1, new object?[] { "data" });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], -1).Should().BeTrue();
    }

    [Fact]
    public void IsCellEmpty_NonEmptyString_ReturnsFalse()
    {
        var table = CreateTable(1, new object?[] { "data" });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 0).Should().BeFalse();
    }

    [Fact]
    public void IsCellEmpty_NumericValue_ReturnsFalse()
    {
        var table = CreateTable(1, new object?[] { 0 });

        LegacyExcelReader.IsCellEmpty(table.Rows[0], 0).Should().BeFalse();
    }

    // =========================================================================
    // InferCellType
    // =========================================================================

    [Fact]
    public void InferCellType_Null_ReturnsEmpty()
    {
        LegacyExcelReader.InferCellType(null).Should().Be("empty");
    }

    [Fact]
    public void InferCellType_DBNull_ReturnsEmpty()
    {
        LegacyExcelReader.InferCellType(DBNull.Value).Should().Be("empty");
    }

    [Fact]
    public void InferCellType_DateTime_ReturnsDate()
    {
        LegacyExcelReader.InferCellType(DateTime.Now).Should().Be("date");
    }

    [Theory]
    [InlineData(42.0)]
    [InlineData(42)]
    [InlineData(42L)]
    public void InferCellType_NumericTypes_ReturnsNumber(object value)
    {
        LegacyExcelReader.InferCellType(value).Should().Be("number");
    }

    [Fact]
    public void InferCellType_Float_ReturnsNumber()
    {
        LegacyExcelReader.InferCellType(3.14f).Should().Be("number");
    }

    [Fact]
    public void InferCellType_Decimal_ReturnsNumber()
    {
        LegacyExcelReader.InferCellType(3.14m).Should().Be("number");
    }

    [Fact]
    public void InferCellType_Boolean_ReturnsBoolean()
    {
        LegacyExcelReader.InferCellType(true).Should().Be("boolean");
    }

    [Fact]
    public void InferCellType_TimeSpan_ReturnsTime()
    {
        LegacyExcelReader.InferCellType(TimeSpan.FromHours(2)).Should().Be("time");
    }

    [Fact]
    public void InferCellType_String_ReturnsText()
    {
        LegacyExcelReader.InferCellType("hello").Should().Be("text");
    }

    // =========================================================================
    // GetLastUsedRow
    // =========================================================================

    [Fact]
    public void GetLastUsedRow_EmptyTable_ReturnsMinusOne()
    {
        var table = CreateTable(3);

        LegacyExcelReader.GetLastUsedRow(table).Should().Be(-1);
    }

    [Fact]
    public void GetLastUsedRow_AllEmptyRows_ReturnsMinusOne()
    {
        var table = CreateTable(2,
            new object?[] { null, null },
            new object?[] { DBNull.Value, "   " });

        LegacyExcelReader.GetLastUsedRow(table).Should().Be(-1);
    }

    [Fact]
    public void GetLastUsedRow_DataInLastRow_ReturnsLastRowIndex()
    {
        var table = CreateTable(2,
            new object?[] { null, null },
            new object?[] { null, null },
            new object?[] { null, "data" });

        LegacyExcelReader.GetLastUsedRow(table).Should().Be(2);
    }

    [Fact]
    public void GetLastUsedRow_DataOnlyInFirstRow_ReturnsZero()
    {
        var table = CreateTable(2,
            new object?[] { "data", null },
            new object?[] { null, null });

        LegacyExcelReader.GetLastUsedRow(table).Should().Be(0);
    }

    // =========================================================================
    // GetLastUsedColumn
    // =========================================================================

    [Fact]
    public void GetLastUsedColumn_EmptyTable_ReturnsMinusOne()
    {
        var table = CreateTable(3);

        LegacyExcelReader.GetLastUsedColumn(table).Should().Be(-1);
    }

    [Fact]
    public void GetLastUsedColumn_DataInLastColumn_ReturnsLastColumnIndex()
    {
        var table = CreateTable(4,
            new object?[] { null, null, null, "data" });

        LegacyExcelReader.GetLastUsedColumn(table).Should().Be(3);
    }

    [Fact]
    public void GetLastUsedColumn_DataOnlyInFirstColumn_ReturnsZero()
    {
        var table = CreateTable(3,
            new object?[] { "data", null, null });

        LegacyExcelReader.GetLastUsedColumn(table).Should().Be(0);
    }

    [Fact]
    public void GetLastUsedColumn_SparseData_ReturnsMaxUsedColumn()
    {
        var table = CreateTable(5,
            new object?[] { "a", null, null, null, null },
            new object?[] { null, null, "b", null, null });

        LegacyExcelReader.GetLastUsedColumn(table).Should().Be(2);
    }

    [Fact]
    public void GetLastUsedColumn_RespectsRowScanLimit()
    {
        var table = CreateTable(3);

        // Add 5 empty rows, then one with data in column 2
        for (int i = 0; i < 5; i++)
        {
            var emptyRow = table.NewRow();
            emptyRow[0] = DBNull.Value;
            emptyRow[1] = DBNull.Value;
            emptyRow[2] = DBNull.Value;
            table.Rows.Add(emptyRow);
        }
        var dataRow = table.NewRow();
        dataRow[0] = DBNull.Value;
        dataRow[1] = DBNull.Value;
        dataRow[2] = "data";
        table.Rows.Add(dataRow);

        // With scanRows=3, should miss the data in row 5
        LegacyExcelReader.GetLastUsedColumn(table, scanRows: 3).Should().Be(-1);

        // With enough rows, should find it
        LegacyExcelReader.GetLastUsedColumn(table, scanRows: 10).Should().Be(2);
    }
}
