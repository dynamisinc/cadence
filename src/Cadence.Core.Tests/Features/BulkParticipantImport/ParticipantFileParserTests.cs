using FluentAssertions;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.Core.Models.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using ClosedXML.Excel;

namespace Cadence.Core.Tests.Features.BulkParticipantImport;

/// <summary>
/// Tests for ParticipantFileParser service.
/// Validates CSV/XLSX parsing, column detection, synonym handling, and row validation.
/// </summary>
public class ParticipantFileParserTests
{
    private readonly ParticipantFileParser _parser;
    private readonly Mock<ILogger<ParticipantFileParser>> _logger;

    public ParticipantFileParserTests()
    {
        _logger = new Mock<ILogger<ParticipantFileParser>>();
        _parser = new ParticipantFileParser(_logger.Object);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private static Stream CreateCsvStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    private static Stream CreateXlsxStream(string[] headers, string[][] rows)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Participants");

        // Write headers
        for (int col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        // Write data rows
        for (int row = 0; row < rows.Length; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                worksheet.Cell(row + 2, col + 1).Value = rows[row][col];
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    // ============================================================================
    // CSV Parsing Tests
    // ============================================================================

    [Fact]
    public async Task ParseAsync_CsvWithExactHeaders_ParsesSuccessfully()
    {
        // Arrange
        var csv = """
            Email,Exercise Role,Display Name,Organization Role
            john@example.com,Controller,John Doe,OrgManager
            jane@example.com,Evaluator,Jane Smith,OrgUser
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(2);
        result.Rows.Should().HaveCount(2);
        result.ColumnMappings.Should().HaveCount(4);

        // Check first row
        var row1 = result.Rows[0];
        row1.Email.Should().Be("john@example.com");
        row1.ExerciseRole.Should().Be("Controller");
        row1.NormalizedExerciseRole.Should().Be(ExerciseRole.Controller);
        row1.DisplayName.Should().Be("John Doe");
        row1.OrganizationRole.Should().Be("OrgManager");
        row1.NormalizedOrgRole.Should().Be(OrgRole.OrgManager);
        row1.IsValid.Should().BeTrue();

        // Check second row
        var row2 = result.Rows[1];
        row2.Email.Should().Be("jane@example.com");
        row2.ExerciseRole.Should().Be("Evaluator");
        row2.NormalizedExerciseRole.Should().Be(ExerciseRole.Evaluator);
    }

    [Fact]
    public async Task ParseAsync_CsvWithSynonymHeaders_MapsCorrectly()
    {
        // Arrange
        var csv = """
            e-mail,hseep role,name,org role
            john@example.com,Director,John Doe,OrgAdmin
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ColumnMappings.Should().Contain(m => m.MappedField == "Email");
        result.ColumnMappings.Should().Contain(m => m.MappedField == "ExerciseRole");
        result.ColumnMappings.Should().Contain(m => m.MappedField == "DisplayName");

        var row = result.Rows[0];
        row.Email.Should().Be("john@example.com");
        row.ExerciseRole.Should().Be("Director");
        row.NormalizedExerciseRole.Should().Be(ExerciseRole.ExerciseDirector);
    }

    [Fact]
    public async Task ParseAsync_CsvWithWhitespaceInHeaders_HandlesCorrectly()
    {
        // Arrange
        var csv = """
            Email Address  ,  Exercise_Role  , Display-Name
            john@example.com,Controller,John Doe
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].Email.Should().Be("john@example.com");
        result.Rows[0].ExerciseRole.Should().Be("Controller");
        result.Rows[0].DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task ParseAsync_CsvSkipsEmptyRows()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,Controller

            jane@example.com,Evaluator
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(2);
        result.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_CsvWithSemicolonDelimiter_DetectsCorrectly()
    {
        // Arrange
        var csv = """
            Email;Exercise Role;Display Name
            john@example.com;Controller;John Doe
            jane@example.com;Evaluator;Jane Smith
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(2);
        result.Rows[0].Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task ParseAsync_CsvWithQuotedFieldsAndCommas_HandlesCorrectly()
    {
        // Arrange
        var csv = """
            Email,Exercise Role,Display Name
            "john@example.com","Controller","Doe, John"
            jane@example.com,Evaluator,"Smith, Jane"
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].DisplayName.Should().Be("Doe, John");
        result.Rows[1].DisplayName.Should().Be("Smith, Jane");
    }

    // ============================================================================
    // XLSX Parsing Tests
    // ============================================================================

    [Fact]
    public async Task ParseAsync_XlsxWithExactHeaders_ParsesSuccessfully()
    {
        // Arrange
        var headers = new[] { "Email", "Exercise Role", "Display Name", "Organization Role" };
        var rows = new[]
        {
            new[] { "john@example.com", "Controller", "John Doe", "OrgManager" },
            new[] { "jane@example.com", "Evaluator", "Jane Smith", "OrgUser" }
        };

        // Act
        var result = await _parser.ParseAsync(CreateXlsxStream(headers, rows), "test.xlsx");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(2);
        result.Rows[0].Email.Should().Be("john@example.com");
        result.Rows[1].Email.Should().Be("jane@example.com");
    }

    [Fact]
    public async Task ParseAsync_XlsxWithPreHeaderRows_DetectsHeaderRow()
    {
        // Arrange
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Participants");

        // Pre-header rows
        worksheet.Cell(1, 1).Value = "Exercise Participant Import";
        worksheet.Cell(2, 1).Value = "Date: 2026-02-09";

        // Actual headers on row 3
        worksheet.Cell(3, 1).Value = "Email";
        worksheet.Cell(3, 2).Value = "Exercise Role";
        worksheet.Cell(3, 3).Value = "Display Name";

        // Data rows
        worksheet.Cell(4, 1).Value = "john@example.com";
        worksheet.Cell(4, 2).Value = "Controller";
        worksheet.Cell(4, 3).Value = "John Doe";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        // Act
        var result = await _parser.ParseAsync(stream, "test.xlsx");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(1);
        result.Rows[0].Email.Should().Be("john@example.com");
    }

    // ============================================================================
    // Validation Tests
    // ============================================================================

    [Fact]
    public async Task ParseAsync_MissingEmailColumn_ReturnsError()
    {
        // Arrange
        var csv = """
            Name,Role
            John Doe,Controller
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Email") && e.Contains("not found"));
    }

    [Fact]
    public async Task ParseAsync_MissingExerciseRoleColumn_ReturnsError()
    {
        // Arrange
        var csv = """
            Email,Name
            john@example.com,John Doe
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Exercise Role") && e.Contains("not found"));
    }

    [Fact]
    public async Task ParseAsync_FileExceeds500Rows_ReturnsError()
    {
        // Arrange
        var sb = new StringBuilder();
        sb.AppendLine("Email,Exercise Role");
        for (int i = 1; i <= 501; i++)
        {
            sb.AppendLine($"user{i}@example.com,Controller");
        }

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(sb.ToString()), "test.csv");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("500") && e.Contains("row"));
    }

    [Fact]
    public async Task ParseAsync_InvalidEmailFormat_CreatesRowError()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            invalid-email,Controller
            john@example.com,Evaluator
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue(); // File-level valid
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ValidationErrors.Should().Contain(e => e.Contains("Invalid email format"));
        result.Rows[1].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_UnrecognizedExerciseRole_CreatesRowError()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,InvalidRole
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ValidationErrors.Should().Contain(e =>
            e.Contains("Invalid exercise role") &&
            e.Contains("InvalidRole"));
    }

    [Fact]
    public async Task ParseAsync_DuplicateEmailInFile_CreatesRowErrors()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,Controller
            jane@example.com,Evaluator
            john@example.com,Observer
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].IsValid.Should().BeFalse();
        result.Rows[0].ValidationErrors.Should().Contain(e => e.Contains("Duplicate email"));
        result.Rows[1].IsValid.Should().BeTrue();
        result.Rows[2].IsValid.Should().BeFalse();
        result.Rows[2].ValidationErrors.Should().Contain(e => e.Contains("Duplicate email"));
    }

    // ============================================================================
    // Role Normalization Tests
    // ============================================================================

    [Fact]
    public async Task ParseAsync_ExerciseRoleSynonyms_NormalizesCorrectly()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            user1@example.com,ExerciseDirector
            user2@example.com,Director
            user3@example.com,ED
            user4@example.com,Controller
            user5@example.com,Ctrl
            user6@example.com,Evaluator
            user7@example.com,Eval
            user8@example.com,Observer
            user9@example.com,Obs
            user10@example.com,Administrator
            user11@example.com,Admin
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].NormalizedExerciseRole.Should().Be(ExerciseRole.ExerciseDirector);
        result.Rows[1].NormalizedExerciseRole.Should().Be(ExerciseRole.ExerciseDirector);
        result.Rows[2].NormalizedExerciseRole.Should().Be(ExerciseRole.ExerciseDirector);
        result.Rows[3].NormalizedExerciseRole.Should().Be(ExerciseRole.Controller);
        result.Rows[4].NormalizedExerciseRole.Should().Be(ExerciseRole.Controller);
        result.Rows[5].NormalizedExerciseRole.Should().Be(ExerciseRole.Evaluator);
        result.Rows[6].NormalizedExerciseRole.Should().Be(ExerciseRole.Evaluator);
        result.Rows[7].NormalizedExerciseRole.Should().Be(ExerciseRole.Observer);
        result.Rows[8].NormalizedExerciseRole.Should().Be(ExerciseRole.Observer);
        result.Rows[9].NormalizedExerciseRole.Should().Be(ExerciseRole.Administrator);
        result.Rows[10].NormalizedExerciseRole.Should().Be(ExerciseRole.Administrator);

        result.Rows.Should().AllSatisfy(r => r.IsValid.Should().BeTrue());
    }

    [Fact]
    public async Task ParseAsync_OrgRoleSynonyms_NormalizesCorrectly()
    {
        // Arrange
        var csv = """
            Email,Exercise Role,Organization Role
            user1@example.com,Controller,OrgAdmin
            user2@example.com,Controller,Org Admin
            user3@example.com,Controller,OrgManager
            user4@example.com,Controller,Org Manager
            user5@example.com,Controller,OrgUser
            user6@example.com,Controller,Org User
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].NormalizedOrgRole.Should().Be(OrgRole.OrgAdmin);
        result.Rows[1].NormalizedOrgRole.Should().Be(OrgRole.OrgAdmin);
        result.Rows[2].NormalizedOrgRole.Should().Be(OrgRole.OrgManager);
        result.Rows[3].NormalizedOrgRole.Should().Be(OrgRole.OrgManager);
        result.Rows[4].NormalizedOrgRole.Should().Be(OrgRole.OrgUser);
        result.Rows[5].NormalizedOrgRole.Should().Be(OrgRole.OrgUser);
    }

    [Fact]
    public async Task ParseAsync_EmptyOrgRole_LeavesNullNormalized()
    {
        // Arrange
        var csv = """
            Email,Exercise Role,Organization Role
            john@example.com,Controller,
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].OrganizationRole.Should().BeNullOrEmpty();
        result.Rows[0].NormalizedOrgRole.Should().BeNull();
        result.Rows[0].IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_UnrecognizedOrgRole_LeavesNullNormalized()
    {
        // Arrange
        var csv = """
            Email,Exercise Role,Organization Role
            john@example.com,Controller,InvalidOrgRole
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].OrganizationRole.Should().Be("InvalidOrgRole");
        result.Rows[0].NormalizedOrgRole.Should().BeNull();
        result.Rows[0].IsValid.Should().BeTrue(); // OrgRole is optional, so invalid value doesn't cause error
    }

    // ============================================================================
    // Session ID and Metadata Tests
    // ============================================================================

    [Fact]
    public async Task ParseAsync_GeneratesUniqueSessionId()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,Controller
            """;

        // Act
        var result1 = await _parser.ParseAsync(CreateCsvStream(csv), "test1.csv");
        var result2 = await _parser.ParseAsync(CreateCsvStream(csv), "test2.csv");

        // Assert
        result1.SessionId.Should().NotBe(Guid.Empty);
        result2.SessionId.Should().NotBe(Guid.Empty);
        result1.SessionId.Should().NotBe(result2.SessionId);
    }

    [Fact]
    public async Task ParseAsync_PreservesFileName()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,Controller
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "participants-import.csv");

        // Assert
        result.FileName.Should().Be("participants-import.csv");
    }

    [Fact]
    public async Task ParseAsync_SetsRowNumbers()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,Controller
            jane@example.com,Evaluator
            bob@example.com,Observer
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.Rows[0].RowNumber.Should().Be(2); // Row 1 is header
        result.Rows[1].RowNumber.Should().Be(3);
        result.Rows[2].RowNumber.Should().Be(4);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsError()
    {
        // Arrange
        var csv = "";

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseAsync_OnlyHeader_ReturnsValidButEmpty()
    {
        // Arrange
        var csv = "Email,Exercise Role";

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalRows.Should().Be(0);
        result.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_UtfBom_HandlesCorrectly()
    {
        // Arrange - UTF-8 BOM is EF BB BF
        var utf8WithBom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = Encoding.UTF8.GetBytes("Email,Exercise Role\njohn@example.com,Controller");
        var combined = utf8WithBom.Concat(csvBytes).ToArray();

        // Act
        var result = await _parser.ParseAsync(new MemoryStream(combined), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task ParseAsync_MultipleColumnsMatchSameField_TakesFirstAndWarns()
    {
        // Arrange
        var csv = """
            Email,E-mail,Exercise Role
            john@example.com,john2@example.com,Controller
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Email") && w.Contains("Multiple"));
        // Should use first matching column
        result.Rows[0].Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task ParseAsync_CaseInsensitiveRoleMatching()
    {
        // Arrange
        var csv = """
            Email,Exercise Role
            john@example.com,controller
            jane@example.com,EVALUATOR
            bob@example.com,Observer
            """;

        // Act
        var result = await _parser.ParseAsync(CreateCsvStream(csv), "test.csv");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Rows[0].NormalizedExerciseRole.Should().Be(ExerciseRole.Controller);
        result.Rows[1].NormalizedExerciseRole.Should().Be(ExerciseRole.Evaluator);
        result.Rows[2].NormalizedExerciseRole.Should().Be(ExerciseRole.Observer);
    }

    [Fact]
    public async Task ParseAsync_UnsupportedExtension_ReturnsError()
    {
        var stream = CreateCsvStream("Email\ntest@test.com");

        var result = await _parser.ParseAsync(stream, "test.txt");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_TabDelimitedCsv_DetectsDelimiter()
    {
        var csv = "Email\tExercise Role\tDisplay Name\nuser@test.com\tController\tJohn Doe";
        var stream = CreateCsvStream(csv);

        var result = await _parser.ParseAsync(stream, "test.csv");

        result.IsValid.Should().BeTrue();
        result.Rows.Should().HaveCount(1);
        result.Rows[0].Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task ParseAsync_ExtraColumns_IgnoredGracefully()
    {
        var csv = "Email,Exercise Role,Display Name,Extra1,Extra2\nuser@test.com,Controller,John Doe,foo,bar";
        var stream = CreateCsvStream(csv);

        var result = await _parser.ParseAsync(stream, "test.csv");

        result.IsValid.Should().BeTrue();
        result.Rows.Should().HaveCount(1);
        result.Rows[0].Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task ParseAsync_LargeFile_ParsesAllRows()
    {
        var sb = new StringBuilder("Email,Exercise Role,Display Name\n");
        for (int i = 0; i < 100; i++)
        {
            sb.AppendLine($"user{i}@test.com,Controller,User {i}");
        }
        var stream = CreateCsvStream(sb.ToString());

        var result = await _parser.ParseAsync(stream, "test.csv");

        result.IsValid.Should().BeTrue();
        result.Rows.Should().HaveCount(100);
    }
}
