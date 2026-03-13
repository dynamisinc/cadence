using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class RowValidationServiceTests
{
    private static ColumnMappingDto RequiredMapping(string field, string displayName, int? sourceCol = 0) => new()
    {
        CadenceField = field,
        DisplayName = displayName,
        IsRequired = true,
        SourceColumnIndex = sourceCol
    };

    private static ColumnMappingDto OptionalMapping(string field, string displayName, int? sourceCol = 0) => new()
    {
        CadenceField = field,
        DisplayName = displayName,
        IsRequired = false,
        SourceColumnIndex = sourceCol
    };

    // =========================================================================
    // ValidateRows Tests
    // =========================================================================

    [Fact]
    public void ValidateRows_EmptyList_ReturnsEmptyResults()
    {
        var rows = new List<Dictionary<string, object?>>();
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRows_RowNumbersStartAt2()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { { "Title", "Row 1" } },
            new() { { "Title", "Row 2" } },
            new() { { "Title", "Row 3" } }
        };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results.Should().HaveCount(3);
        results[0].RowNumber.Should().Be(2);
        results[1].RowNumber.Should().Be(3);
        results[2].RowNumber.Should().Be(4);
    }

    [Fact]
    public void ValidateRows_ValidRow_StatusIsValid()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { { "Title", "Test Inject" }, { "ScheduledTime", "09:00" } }
        };
        var mappings = new List<ColumnMappingDto>
        {
            RequiredMapping("Title", "Title"),
            OptionalMapping("ScheduledTime", "Scheduled Time")
        };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results[0].Status.Should().Be("Valid");
        results[0].Issues.Should().BeNull();
    }

    [Fact]
    public void ValidateRows_RowWithError_StatusIsError()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { { "Title", null } } // Missing required field
        };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results[0].Status.Should().Be("Error");
    }

    [Fact]
    public void ValidateRows_RowWithWarningOnly_StatusIsWarning()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { { "Title", "Test" }, { "Priority", "99" } } // Invalid priority = warning
        };
        var mappings = new List<ColumnMappingDto>
        {
            RequiredMapping("Title", "Title"),
            OptionalMapping("Priority", "Priority")
        };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results[0].Status.Should().Be("Warning");
    }

    [Fact]
    public void ValidateRows_PreservesRowValues()
    {
        var row = new Dictionary<string, object?> { { "Title", "My Inject" } };
        var rows = new List<Dictionary<string, object?>> { row };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var results = RowValidationService.ValidateRows(rows, mappings);

        results[0].Values.Should().BeSameAs(row);
    }

    // =========================================================================
    // ValidateSingleRow — Required Field Tests
    // =========================================================================

    [Fact]
    public void ValidateSingleRow_MissingRequiredField_ReturnsError()
    {
        var row = new Dictionary<string, object?>();
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "Title" && i.Severity == "Error");
        issues.First(i => i.Field == "Title").Message.Should().Contain("required");
    }

    [Fact]
    public void ValidateSingleRow_NullRequiredField_ReturnsError()
    {
        var row = new Dictionary<string, object?> { { "Title", null } };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "Title" && i.Severity == "Error");
    }

    [Fact]
    public void ValidateSingleRow_WhitespaceRequiredField_ReturnsError()
    {
        var row = new Dictionary<string, object?> { { "Title", "   " } };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "Title" && i.Severity == "Error");
    }

    [Fact]
    public void ValidateSingleRow_PresentRequiredField_NoError()
    {
        var row = new Dictionary<string, object?> { { "Title", "Valid Title" } };
        var mappings = new List<ColumnMappingDto> { RequiredMapping("Title", "Title") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "Title").Should().BeEmpty();
    }

    [Fact]
    public void ValidateSingleRow_OptionalFieldMissing_NoError()
    {
        var row = new Dictionary<string, object?>();
        var mappings = new List<ColumnMappingDto> { OptionalMapping("Notes", "Notes") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "Notes").Should().BeEmpty();
    }

    // =========================================================================
    // ValidateSingleRow — ScheduledTime Validation
    // =========================================================================

    [Theory]
    [InlineData("09:00")]
    [InlineData("14:30")]
    [InlineData("9:00 AM")]
    public void ValidateSingleRow_ValidScheduledTime_NoError(string timeValue)
    {
        var row = new Dictionary<string, object?> { { "ScheduledTime", timeValue } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("ScheduledTime", "Scheduled Time") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "ScheduledTime" && i.Severity == "Error").Should().BeEmpty();
    }

    [Fact]
    public void ValidateSingleRow_InvalidScheduledTime_ReturnsError()
    {
        var row = new Dictionary<string, object?> { { "ScheduledTime", "not a time" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("ScheduledTime", "Scheduled Time") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "ScheduledTime" && i.Severity == "Error");
    }

    [Fact]
    public void ValidateSingleRow_MissingScheduledTime_ReturnsWarning()
    {
        var row = new Dictionary<string, object?>();
        var mappings = new List<ColumnMappingDto> { OptionalMapping("ScheduledTime", "Scheduled Time", sourceCol: null) };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "ScheduledTime" && i.Severity == "Warning");
    }

    [Fact]
    public void ValidateSingleRow_EmptyScheduledTime_ReturnsWarning()
    {
        var row = new Dictionary<string, object?> { { "ScheduledTime", "" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("ScheduledTime", "Scheduled Time") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "ScheduledTime" && i.Severity == "Warning");
    }

    // =========================================================================
    // ValidateSingleRow — Priority Validation
    // =========================================================================

    [Theory]
    [InlineData("1")]
    [InlineData("3")]
    [InlineData("5")]
    public void ValidateSingleRow_ValidPriority_NoWarning(string priority)
    {
        var row = new Dictionary<string, object?> { { "Priority", priority } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("Priority", "Priority") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "Priority").Should().BeEmpty();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void ValidateSingleRow_InvalidPriority_ReturnsWarning(string priority)
    {
        var row = new Dictionary<string, object?> { { "Priority", priority } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("Priority", "Priority") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "Priority" && i.Severity == "Warning");
    }

    // =========================================================================
    // ValidateSingleRow — InjectType Validation
    // =========================================================================

    [Fact]
    public void ValidateSingleRow_RecognizedInjectType_NoWarning()
    {
        var row = new Dictionary<string, object?> { { "InjectType", "standard" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("InjectType", "Inject Type") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "InjectType").Should().BeEmpty();
    }

    [Fact]
    public void ValidateSingleRow_TriggerTypeLikeInjectType_ReturnsWarning()
    {
        var row = new Dictionary<string, object?> { { "InjectType", "controller action" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("InjectType", "Inject Type") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "InjectType" && i.Severity == "Warning" && i.Message.Contains("Trigger Type"));
    }

    [Fact]
    public void ValidateSingleRow_DeliveryMethodLikeInjectType_ReturnsWarning()
    {
        var row = new Dictionary<string, object?> { { "InjectType", "email" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("InjectType", "Inject Type") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "InjectType" && i.Severity == "Warning" && i.Message.Contains("Delivery Method"));
    }

    // =========================================================================
    // ValidateSingleRow — TriggerType Validation
    // =========================================================================

    [Fact]
    public void ValidateSingleRow_RecognizedTriggerType_NoWarning()
    {
        var row = new Dictionary<string, object?> { { "TriggerType", "manual" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("TriggerType", "Trigger Type") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Where(i => i.Field == "TriggerType").Should().BeEmpty();
    }

    [Fact]
    public void ValidateSingleRow_UnrecognizedTriggerType_ReturnsWarning()
    {
        var row = new Dictionary<string, object?> { { "TriggerType", "unknown trigger" } };
        var mappings = new List<ColumnMappingDto> { OptionalMapping("TriggerType", "Trigger Type") };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().Contain(i => i.Field == "TriggerType" && i.Severity == "Warning" && i.Message.Contains("Unrecognized"));
    }

    // =========================================================================
    // ValidateSingleRow — Multiple Issues
    // =========================================================================

    [Fact]
    public void ValidateSingleRow_MultipleIssues_ReturnsAll()
    {
        var row = new Dictionary<string, object?>
        {
            { "Title", null },           // Missing required
            { "Priority", "99" },        // Invalid priority
            { "TriggerType", "xyz" }     // Unrecognized trigger
        };
        var mappings = new List<ColumnMappingDto>
        {
            RequiredMapping("Title", "Title"),
            OptionalMapping("Priority", "Priority"),
            OptionalMapping("TriggerType", "Trigger Type")
        };

        var issues = RowValidationService.ValidateSingleRow(row, mappings);

        issues.Should().HaveCountGreaterOrEqualTo(3);
        issues.Should().Contain(i => i.Field == "Title" && i.Severity == "Error");
        issues.Should().Contain(i => i.Field == "Priority" && i.Severity == "Warning");
        issues.Should().Contain(i => i.Field == "TriggerType" && i.Severity == "Warning");
    }

    // =========================================================================
    // IsEmpty Tests
    // =========================================================================

    [Fact]
    public void IsEmpty_Null_ReturnsTrue()
    {
        RowValidationService.IsEmpty(null).Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_EmptyString_ReturnsTrue()
    {
        RowValidationService.IsEmpty("").Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WhitespaceString_ReturnsTrue()
    {
        RowValidationService.IsEmpty("   ").Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NonEmptyString_ReturnsFalse()
    {
        RowValidationService.IsEmpty("hello").Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_NonStringObject_ReturnsFalse()
    {
        RowValidationService.IsEmpty(42).Should().BeFalse();
    }
}
