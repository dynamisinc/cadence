using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Features.ExcelImport.Services;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class ColumnMappingStrategyTests
{
    private static ColumnInfoDto Col(int index, string header) => new()
    {
        Index = index,
        Letter = ((char)('A' + index)).ToString(),
        Header = header,
        DataType = "text",
        SampleValues = Array.Empty<string>(),
        FillRate = 100
    };

    // =========================================================================
    // FindBestMatchingColumn — Exact Match Tests
    // =========================================================================

    [Theory]
    [InlineData("Title", "title", 100)]
    [InlineData("Title", "name", 100)]
    [InlineData("Description", "description", 100)]
    [InlineData("Description", "narrative", 100)]
    [InlineData("InjectNumber", "#", 100)]
    [InlineData("ScheduledTime", "time", 100)]
    [InlineData("Source", "source", 100)]
    [InlineData("Target", "target", 100)]
    [InlineData("Phase", "phase", 100)]
    [InlineData("Priority", "priority", 100)]
    [InlineData("Notes", "notes", 100)]
    public void FindBestMatchingColumn_ExactMatch_ReturnsConfidence100(string fieldName, string header, int expectedConfidence)
    {
        var columns = new List<ColumnInfoDto> { Col(0, header) };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn(fieldName, columns);

        index.Should().Be(0);
        confidence.Should().Be(expectedConfidence);
    }

    [Fact]
    public void FindBestMatchingColumn_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var columns = new List<ColumnInfoDto> { Col(0, "TITLE") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(0);
        confidence.Should().Be(100);
    }

    // =========================================================================
    // FindBestMatchingColumn — Contains Match Tests
    // =========================================================================

    [Theory]
    [InlineData("Title", "inject title", 80)]       // "inject title" contains "title" pattern → 80
    [InlineData("InjectNumber", "inject number", 80)] // "inject number" contains "number" pattern → 80
    [InlineData("ScheduledTime", "scheduled time", 80)] // "scheduled time" contains "time" pattern → 80
    [InlineData("InjectType", "inject type", 100)]   // "inject type" is first pattern — exact match
    [InlineData("TriggerType", "trigger type", 80)]  // "trigger type" contains "trigger" → 80
    public void FindBestMatchingColumn_MultiWordHeader_MatchesViaShorterPatternContains(string fieldName, string header, int expectedConfidence)
    {
        // Multi-word headers often match a shorter pattern via contains before the
        // exact multi-word pattern is reached, resulting in confidence 80.
        var columns = new List<ColumnInfoDto> { Col(0, header) };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn(fieldName, columns);

        index.Should().Be(0);
        confidence.Should().Be(expectedConfidence);
    }

    [Fact]
    public void FindBestMatchingColumn_ContainsMatch_ReturnsConfidence80()
    {
        // "inject title (combined)" contains "title"
        var columns = new List<ColumnInfoDto> { Col(0, "inject title (combined)") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(0);
        confidence.Should().Be(80);
    }

    [Fact]
    public void FindBestMatchingColumn_ContainsMatch_DescriptionInLongerHeader()
    {
        var columns = new List<ColumnInfoDto> { Col(0, "full description of scenario") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Description", columns);

        index.Should().Be(0);
        confidence.Should().Be(80);
    }

    // =========================================================================
    // FindBestMatchingColumn — No Match Tests
    // =========================================================================

    [Fact]
    public void FindBestMatchingColumn_NoMatch_ReturnsNegativeIndex()
    {
        var columns = new List<ColumnInfoDto> { Col(0, "xyz_unrelated") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(-1);
        confidence.Should().Be(0);
    }

    [Fact]
    public void FindBestMatchingColumn_UnknownFieldName_ReturnsNegativeIndex()
    {
        var columns = new List<ColumnInfoDto> { Col(0, "title") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("NonExistentField", columns);

        index.Should().Be(-1);
        confidence.Should().Be(0);
    }

    [Fact]
    public void FindBestMatchingColumn_EmptyColumns_ReturnsNegativeIndex()
    {
        var columns = new List<ColumnInfoDto>();

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(-1);
        confidence.Should().Be(0);
    }

    // =========================================================================
    // FindBestMatchingColumn — Multiple Columns
    // =========================================================================

    [Fact]
    public void FindBestMatchingColumn_MultipleColumns_ReturnsFirstMatch()
    {
        var columns = new List<ColumnInfoDto>
        {
            Col(0, "something else"),
            Col(1, "title"),
            Col(2, "also a title column")
        };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(1);
        confidence.Should().Be(100);
    }

    [Fact]
    public void FindBestMatchingColumn_ExactMatchBeforeContains_PrefersExact()
    {
        // The algorithm iterates columns then patterns.
        // "title" will match column index 1 exactly before checking contains on column 2.
        var columns = new List<ColumnInfoDto>
        {
            Col(0, "something"),
            Col(1, "title"),
        };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Title", columns);

        index.Should().Be(1);
        confidence.Should().Be(100);
    }

    [Fact]
    public void FindBestMatchingColumn_ReturnsCorrectColumnIndex()
    {
        var columns = new List<ColumnInfoDto>
        {
            Col(0, "Col A"),
            Col(1, "Col B"),
            Col(2, "Col C"),
            Col(3, "description"),
            Col(4, "Col E"),
        };

        var (index, _) = ColumnMappingStrategy.FindBestMatchingColumn("Description", columns);

        index.Should().Be(3);
    }

    // =========================================================================
    // InjectTypeSynonyms Tests
    // =========================================================================

    [Theory]
    [InlineData("standard", InjectType.Standard)]
    [InlineData("normal", InjectType.Standard)]
    [InlineData("scheduled", InjectType.Standard)]
    [InlineData("contingency", InjectType.Contingency)]
    [InlineData("backup", InjectType.Contingency)]
    [InlineData("adaptive", InjectType.Adaptive)]
    [InlineData("branch", InjectType.Adaptive)]
    [InlineData("complexity", InjectType.Complexity)]
    [InlineData("escalation", InjectType.Complexity)]
    [InlineData("administrative", InjectType.Standard)]
    [InlineData("contingent", InjectType.Contingency)]
    public void InjectTypeSynonyms_MapsCorrectly(string synonym, InjectType expected)
    {
        ColumnMappingStrategy.InjectTypeSynonyms.Should().ContainKey(synonym);
        ColumnMappingStrategy.InjectTypeSynonyms[synonym].Should().Be(expected);
    }

    [Fact]
    public void InjectTypeSynonyms_IsCaseInsensitive()
    {
        ColumnMappingStrategy.InjectTypeSynonyms.Should().ContainKey("STANDARD");
        ColumnMappingStrategy.InjectTypeSynonyms.Should().ContainKey("Standard");
        ColumnMappingStrategy.InjectTypeSynonyms.Should().ContainKey("standard");
    }

    // =========================================================================
    // TriggerTypeSynonyms Tests
    // =========================================================================

    [Theory]
    [InlineData("manual", TriggerType.Manual)]
    [InlineData("controller", TriggerType.Manual)]
    [InlineData("controller action", TriggerType.Manual)]
    [InlineData("scheduled", TriggerType.Scheduled)]
    [InlineData("auto", TriggerType.Scheduled)]
    [InlineData("automatic", TriggerType.Scheduled)]
    [InlineData("conditional", TriggerType.Conditional)]
    [InlineData("player action", TriggerType.Conditional)]
    [InlineData("event-based", TriggerType.Conditional)]
    public void TriggerTypeSynonyms_MapsCorrectly(string synonym, TriggerType expected)
    {
        ColumnMappingStrategy.TriggerTypeSynonyms.Should().ContainKey(synonym);
        ColumnMappingStrategy.TriggerTypeSynonyms[synonym].Should().Be(expected);
    }

    // =========================================================================
    // DeliveryMethodLikeValues Tests
    // =========================================================================

    [Theory]
    [InlineData("radio")]
    [InlineData("phone")]
    [InlineData("email")]
    [InlineData("verbal")]
    [InlineData("fax")]
    [InlineData("simulation")]
    public void DeliveryMethodLikeValues_ContainsExpectedValues(string value)
    {
        ColumnMappingStrategy.DeliveryMethodLikeValues.Should().Contain(value);
    }

    [Fact]
    public void DeliveryMethodLikeValues_IsCaseInsensitive()
    {
        ColumnMappingStrategy.DeliveryMethodLikeValues.Should().Contain("RADIO");
        ColumnMappingStrategy.DeliveryMethodLikeValues.Should().Contain("Radio");
    }

    // =========================================================================
    // TriggerTypeLikeValues Tests
    // =========================================================================

    [Theory]
    [InlineData("controller action")]
    [InlineData("player action")]
    [InlineData("manual")]
    [InlineData("automatic")]
    [InlineData("scheduled")]
    [InlineData("conditional")]
    public void TriggerTypeLikeValues_ContainsExpectedValues(string value)
    {
        ColumnMappingStrategy.TriggerTypeLikeValues.Should().Contain(value);
    }

    // =========================================================================
    // DeliveryMethodSynonyms Tests
    // =========================================================================

    [Theory]
    [InlineData("in person", "Verbal")]
    [InlineData("face to face", "Verbal")]
    [InlineData("call", "Phone")]
    [InlineData("telephone", "Phone")]
    [InlineData("sms", "Phone")]
    [InlineData("e-mail", "Email")]
    [InlineData("fax", "Written")]
    [InlineData("document", "Written")]
    [InlineData("sim", "Simulation")]
    [InlineData("cax", "Simulation")]
    public void DeliveryMethodSynonyms_MapsCorrectly(string synonym, string expected)
    {
        ColumnMappingStrategy.DeliveryMethodSynonyms.Should().ContainKey(synonym);
        ColumnMappingStrategy.DeliveryMethodSynonyms[synonym].Should().Be(expected);
    }

    // =========================================================================
    // ColumnPatterns Coverage Tests
    // =========================================================================

    [Fact]
    public void ColumnPatterns_CoversAllExpectedFields()
    {
        var expectedFields = new[]
        {
            "InjectNumber", "Title", "Description", "ScheduledTime",
            "ScenarioDay", "ScenarioTime", "Source", "Target", "Track",
            "DeliveryMethod", "ExpectedAction", "Notes", "Phase",
            "Priority", "LocationName", "LocationType",
            "ResponsibleController", "InjectType", "TriggerType"
        };

        foreach (var field in expectedFields)
        {
            ColumnMappingStrategy.ColumnPatterns.Should().ContainKey(field);
        }
    }

    [Fact]
    public void ColumnPatterns_EachFieldHasAtLeastOnePattern()
    {
        foreach (var kvp in ColumnMappingStrategy.ColumnPatterns)
        {
            kvp.Value.Should().NotBeEmpty($"field '{kvp.Key}' should have at least one pattern");
        }
    }

    [Fact]
    public void FindBestMatchingColumn_ExactFieldName_ReturnsHighConfidence()
    {
        var columns = new List<ColumnInfoDto> { Col(0, "inject number") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("InjectNumber", columns);

        index.Should().Be(0);
        confidence.Should().BeGreaterOrEqualTo(80);
    }

    [Fact]
    public void FindBestMatchingColumn_SynonymMatch_ReturnsModerateConfidence()
    {
        var columns = new List<ColumnInfoDto> { Col(2, "track/storyline/thread") };

        var (index, confidence) = ColumnMappingStrategy.FindBestMatchingColumn("Track", columns);

        index.Should().Be(2);
        confidence.Should().Be(80);
    }
}
