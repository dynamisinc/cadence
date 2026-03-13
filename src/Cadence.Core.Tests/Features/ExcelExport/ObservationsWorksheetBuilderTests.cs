using Cadence.Core.Features.ExcelExport.Builders;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelExport;

/// <summary>
/// Tests for ObservationsWorksheetBuilder — Excel worksheet generation for observation data.
/// </summary>
public class ObservationsWorksheetBuilderTests
{
    private static Observation CreateObservation(
        string content = "Test observation",
        ObservationRating? rating = ObservationRating.Performed,
        string? recommendation = "Test recommendation",
        string? location = "Main EOC",
        Inject? inject = null,
        Objective? objective = null,
        ApplicationUser? createdByUser = null)
    {
        return new Observation
        {
            Id = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            Content = content,
            Rating = rating,
            Recommendation = recommendation,
            Location = location,
            ObservedAt = new DateTime(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc),
            Inject = inject,
            Objective = objective,
            CreatedByUser = createdByUser,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
    }

    [Fact]
    public void AddObservationsWorksheet_EmptyList_CreatesWorksheetWithHeaderOnly()
    {
        using var workbook = new XLWorkbook();

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, new List<Observation>(), true);

        var ws = workbook.Worksheet("Observations");
        ws.Should().NotBeNull();
        ws.Cell(1, 1).GetString().Should().Be("Timestamp");
        ws.Cell(2, 1).GetString().Should().BeEmpty();
    }

    [Fact]
    public void AddObservationsWorksheet_SingleObservation_PopulatesAllColumns()
    {
        using var workbook = new XLWorkbook();
        var inject = new Inject { InjectNumber = 5, Title = "Radio Check" };
        var objective = new Objective { Name = "Communications", ObjectiveNumber = "OBJ-1" };
        var user = new ApplicationUser { DisplayName = "Jane Eval" };

        var obs = CreateObservation(
            content: "Clear comms observed",
            rating: ObservationRating.Performed,
            recommendation: "Continue protocol",
            location: "Field HQ",
            inject: inject,
            objective: objective,
            createdByUser: user);

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, new List<Observation> { obs }, false);

        var ws = workbook.Worksheet("Observations");
        ws.Cell(2, 1).GetString().Should().Contain("2026-03-01"); // Timestamp
        ws.Cell(2, 2).GetString().Should().Be("Jane Eval");       // Observer
        ws.Cell(2, 3).GetString().Should().Contain("#5 - Radio Check"); // Inject
        ws.Cell(2, 4).GetString().Should().Be("Clear comms observed");  // Content
        ws.Cell(2, 5).GetString().Should().Contain("Performed");        // Rating
        ws.Cell(2, 6).GetString().Should().Be("Continue protocol");     // Recommendation
        ws.Cell(2, 7).GetString().Should().Be("Field HQ");             // Location
        ws.Cell(2, 8).GetString().Should().Be("Communications");       // Objective
    }

    [Fact]
    public void AddObservationsWorksheet_NoInject_ShowsGeneral()
    {
        using var workbook = new XLWorkbook();
        var obs = CreateObservation(inject: null);

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, new List<Observation> { obs }, false);

        var ws = workbook.Worksheet("Observations");
        ws.Cell(2, 3).GetString().Should().Be("General");
    }

    [Fact]
    public void AddObservationsWorksheet_NullFields_EmptyStrings()
    {
        using var workbook = new XLWorkbook();
        var obs = CreateObservation(
            recommendation: null,
            location: null,
            createdByUser: null);

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, new List<Observation> { obs }, false);

        var ws = workbook.Worksheet("Observations");
        ws.Cell(2, 2).GetString().Should().BeEmpty(); // No observer
        ws.Cell(2, 6).GetString().Should().BeEmpty(); // No recommendation
        ws.Cell(2, 7).GetString().Should().BeEmpty(); // No location
        ws.Cell(2, 8).GetString().Should().BeEmpty(); // No objective
    }

    [Fact]
    public void AddObservationsWorksheet_MultipleObservations_AllRowsPopulated()
    {
        using var workbook = new XLWorkbook();
        var observations = new List<Observation>
        {
            CreateObservation(content: "First"),
            CreateObservation(content: "Second"),
            CreateObservation(content: "Third")
        };

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, observations, false);

        var ws = workbook.Worksheet("Observations");
        ws.Cell(2, 4).GetString().Should().Be("First");
        ws.Cell(3, 4).GetString().Should().Be("Second");
        ws.Cell(4, 4).GetString().Should().Be("Third");
    }

    [Fact]
    public void AddObservationsWorksheet_WithFormatting_AppliesAlternatingColors()
    {
        using var workbook = new XLWorkbook();
        var observations = new List<Observation>
        {
            CreateObservation(content: "Row 2"),
            CreateObservation(content: "Row 3")
        };

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, observations, true);

        var ws = workbook.Worksheet("Observations");
        // Row 2 (even) should have background color
        ws.Cell(2, 1).Style.Fill.BackgroundColor.Should().Be(XLColor.LightGoldenrodYellow);
        // Row 3 (odd) should not
        ws.Cell(3, 1).Style.Fill.BackgroundColor.Should().NotBe(XLColor.LightGoldenrodYellow);
    }

    [Fact]
    public void AddObservationsWorksheet_WithFormatting_SetsAutoFilter()
    {
        using var workbook = new XLWorkbook();
        var observations = new List<Observation> { CreateObservation() };

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, observations, true);

        var ws = workbook.Worksheet("Observations");
        ws.AutoFilter.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AddObservationsWorksheet_WithoutFormatting_NoAutoFilter()
    {
        using var workbook = new XLWorkbook();
        var observations = new List<Observation> { CreateObservation() };

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, observations, false);

        var ws = workbook.Worksheet("Observations");
        ws.AutoFilter.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AddObservationsWorksheet_SpecialCharactersInContent_PreservesRawText()
    {
        using var workbook = new XLWorkbook();
        var obs = CreateObservation(
            content: "Comms failed: \"channel 7\" & backup <EOC> unavailable",
            recommendation: "Use channel 7 backup; notify all units");

        ObservationsWorksheetBuilder.AddObservationsWorksheet(workbook, new List<Observation> { obs }, false);

        var ws = workbook.Worksheet("Observations");
        ws.Cell(2, 4).GetString().Should().Be("Comms failed: \"channel 7\" & backup <EOC> unavailable");
        ws.Cell(2, 6).GetString().Should().Be("Use channel 7 backup; notify all units");
    }
}
