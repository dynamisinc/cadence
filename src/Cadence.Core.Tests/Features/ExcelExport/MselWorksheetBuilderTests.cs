using System.Globalization;
using Cadence.Core.Features.ExcelExport.Builders;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelExport;

/// <summary>
/// Tests for MselWorksheetBuilder — Excel worksheet and CSV generation for MSEL inject data.
/// </summary>
public class MselWorksheetBuilderTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static Inject CreateInject(
        int injectNumber = 1,
        string title = "Test Inject",
        string description = "Test Description",
        TimeOnly? scheduledTime = null,
        int? scenarioDay = null,
        TimeOnly? scenarioTime = null,
        string? source = null,
        string target = "EOC",
        string? track = null,
        Phase? phase = null,
        string? expectedAction = null,
        string? controllerNotes = null,
        int? priority = null,
        string? locationName = null,
        string? responsibleController = null,
        InjectStatus status = InjectStatus.Draft,
        DateTime? firedAt = null,
        ApplicationUser? firedByUser = null,
        DeliveryMethodLookup? deliveryMethodLookup = null)
    {
        return new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = injectNumber,
            Title = title,
            Description = description,
            ScheduledTime = scheduledTime ?? new TimeOnly(9, 0),
            ScenarioDay = scenarioDay,
            ScenarioTime = scenarioTime,
            Source = source,
            Target = target,
            Track = track,
            Phase = phase,
            ExpectedAction = expectedAction,
            ControllerNotes = controllerNotes,
            Priority = priority,
            LocationName = locationName,
            ResponsibleController = responsibleController,
            Status = status,
            FiredAt = firedAt,
            FiredByUser = firedByUser,
            DeliveryMethodLookup = deliveryMethodLookup,
            MselId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
    }

    private static Phase CreatePhase(string name = "Initial Response", int sequence = 1)
    {
        return new Phase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Sequence = sequence,
            OrganizationId = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
    }

    // =========================================================================
    // MSEL Worksheet — Headers
    // =========================================================================

    [Fact]
    public void AddMselWorksheet_EmptyList_CreatesWorksheetWithHeaderOnly()
    {
        using var workbook = new XLWorkbook();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject>(), true, false);

        var ws = workbook.Worksheet("MSEL");
        ws.Should().NotBeNull();
        ws.Cell(1, 1).GetString().Should().Be("Inject #");
        ws.Cell(2, 1).GetString().Should().BeEmpty();
    }

    [Fact]
    public void AddMselWorksheet_EmptyList_WritesAllCoreHeaders()
    {
        using var workbook = new XLWorkbook();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject>(), true, false);

        var ws = workbook.Worksheet("MSEL");
        // Spot-check a selection of expected header labels from MselColumns
        ws.Cell(1, 1).GetString().Should().Be("Inject #");
        ws.Cell(1, 2).GetString().Should().Be("Title");
        ws.Cell(1, 3).GetString().Should().Be("Description");
        ws.Cell(1, 4).GetString().Should().Be("Scheduled Time");
        ws.Cell(1, 16).GetString().Should().Be("Responsible Controller");
    }

    // =========================================================================
    // MSEL Worksheet — Data Rows
    // =========================================================================

    [Fact]
    public void AddMselWorksheet_SingleInject_PopulatesAllCoreColumns()
    {
        using var workbook = new XLWorkbook();
        var phase = CreatePhase("Response Phase");
        var inject = CreateInject(
            injectNumber: 3,
            title: "EOC Notification",
            description: "Notify EOC of incident",
            scheduledTime: new TimeOnly(10, 30),
            scenarioDay: 2,
            scenarioTime: new TimeOnly(8, 0),
            source: "Dispatch",
            target: "EOC Director",
            track: "Fire",
            phase: phase,
            expectedAction: "Activate EOC",
            controllerNotes: "Wait for acknowledgement",
            priority: 1,
            locationName: "EOC Main",
            responsibleController: "Jane Smith");

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, false, false);

        var ws = workbook.Worksheet("MSEL");
        ws.Cell(2, 1).GetString().Should().Be("3");                       // InjectNumber
        ws.Cell(2, 2).GetString().Should().Be("EOC Notification");        // Title
        ws.Cell(2, 3).GetString().Should().Be("Notify EOC of incident");  // Description
        ws.Cell(2, 4).GetString().Should().Be("10:30");                   // ScheduledTime
        ws.Cell(2, 5).GetString().Should().Be("2");                       // ScenarioDay
        ws.Cell(2, 6).GetString().Should().Be("08:00");                   // ScenarioTime
        ws.Cell(2, 7).GetString().Should().Be("Dispatch");                // Source
        ws.Cell(2, 8).GetString().Should().Be("EOC Director");            // Target
        ws.Cell(2, 10).GetString().Should().Be("Fire");                   // Track
        ws.Cell(2, 11).GetString().Should().Be("Response Phase");         // Phase
        ws.Cell(2, 12).GetString().Should().Be("Activate EOC");           // ExpectedAction
        ws.Cell(2, 13).GetString().Should().Be("Wait for acknowledgement"); // ControllerNotes
        ws.Cell(2, 14).GetString().Should().Be("1");                      // Priority
        ws.Cell(2, 15).GetString().Should().Be("EOC Main");               // LocationName
        ws.Cell(2, 16).GetString().Should().Be("Jane Smith");             // ResponsibleController
    }

    [Fact]
    public void AddMselWorksheet_MultipleInjects_AllRowsPopulated()
    {
        using var workbook = new XLWorkbook();
        var injects = new List<Inject>
        {
            CreateInject(injectNumber: 1, title: "First"),
            CreateInject(injectNumber: 2, title: "Second"),
            CreateInject(injectNumber: 3, title: "Third")
        };

        MselWorksheetBuilder.AddMselWorksheet(workbook, injects, false, false);

        var ws = workbook.Worksheet("MSEL");
        ws.Cell(2, 2).GetString().Should().Be("First");
        ws.Cell(3, 2).GetString().Should().Be("Second");
        ws.Cell(4, 2).GetString().Should().Be("Third");
    }

    [Fact]
    public void AddMselWorksheet_NullOptionalFields_WritesEmptyStrings()
    {
        using var workbook = new XLWorkbook();
        var inject = CreateInject(
            scenarioDay: null,
            scenarioTime: null,
            source: null,
            track: null,
            phase: null,
            expectedAction: null,
            controllerNotes: null,
            priority: null,
            locationName: null,
            responsibleController: null);

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, false, false);

        var ws = workbook.Worksheet("MSEL");
        ws.Cell(2, 5).GetString().Should().BeEmpty();  // ScenarioDay
        ws.Cell(2, 6).GetString().Should().BeEmpty();  // ScenarioTime
        ws.Cell(2, 7).GetString().Should().BeEmpty();  // Source
        ws.Cell(2, 10).GetString().Should().BeEmpty(); // Track
        ws.Cell(2, 11).GetString().Should().BeEmpty(); // Phase
        ws.Cell(2, 12).GetString().Should().BeEmpty(); // ExpectedAction
        ws.Cell(2, 13).GetString().Should().BeEmpty(); // ControllerNotes
        ws.Cell(2, 14).GetString().Should().BeEmpty(); // Priority
        ws.Cell(2, 15).GetString().Should().BeEmpty(); // LocationName
        ws.Cell(2, 16).GetString().Should().BeEmpty(); // ResponsibleController
    }

    // =========================================================================
    // MSEL Worksheet — Formatting
    // =========================================================================

    [Fact]
    public void AddMselWorksheet_WithFormatting_AppliesHeaderStyleAndWrapText()
    {
        using var workbook = new XLWorkbook();
        var inject = CreateInject();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, true, false);

        var ws = workbook.Worksheet("MSEL");
        ws.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        ws.Cell(1, 1).Style.Fill.BackgroundColor.Should().Be(XLColor.LightBlue);
        ws.Row(1).Style.Alignment.WrapText.Should().BeTrue();
    }

    [Fact]
    public void AddMselWorksheet_WithFormatting_AppliesAlternatingRowColors()
    {
        using var workbook = new XLWorkbook();
        var injects = new List<Inject>
        {
            CreateInject(injectNumber: 1, title: "Row 2"),
            CreateInject(injectNumber: 2, title: "Row 3")
        };

        MselWorksheetBuilder.AddMselWorksheet(workbook, injects, true, false);

        var ws = workbook.Worksheet("MSEL");
        // Row 2 (even) should have AliceBlue background
        ws.Cell(2, 1).Style.Fill.BackgroundColor.Should().Be(XLColor.AliceBlue);
        // Row 3 (odd) should not
        ws.Cell(3, 1).Style.Fill.BackgroundColor.Should().NotBe(XLColor.AliceBlue);
    }

    [Fact]
    public void AddMselWorksheet_WithFormatting_SetsAutoFilter()
    {
        using var workbook = new XLWorkbook();
        var inject = CreateInject();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, true, false);

        var ws = workbook.Worksheet("MSEL");
        ws.AutoFilter.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AddMselWorksheet_WithoutFormatting_NoAutoFilter()
    {
        using var workbook = new XLWorkbook();
        var inject = CreateInject();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, false, false);

        var ws = workbook.Worksheet("MSEL");
        ws.AutoFilter.IsEnabled.Should().BeFalse();
    }

    // =========================================================================
    // MSEL Worksheet — Conduct Columns
    // =========================================================================

    [Fact]
    public void AddMselWorksheet_WithConductData_AppendsConductHeaders()
    {
        using var workbook = new XLWorkbook();

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject>(), true, true);

        var ws = workbook.Worksheet("MSEL");
        // Core columns = 16, so conduct columns start at 17
        ws.Cell(1, 17).GetString().Should().Be("Status");
        ws.Cell(1, 18).GetString().Should().Be("Fired At");
        ws.Cell(1, 19).GetString().Should().Be("Fired By");
    }

    [Fact]
    public void AddMselWorksheet_WithConductData_WritesConductValues()
    {
        using var workbook = new XLWorkbook();
        var firedAt = new DateTime(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc);
        var firedByUser = new ApplicationUser { DisplayName = "John Controller" };
        var inject = CreateInject(
            status: InjectStatus.Released,
            firedAt: firedAt,
            firedByUser: firedByUser);

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, false, true);

        var ws = workbook.Worksheet("MSEL");
        ws.Cell(2, 17).GetString().Should().Be("Released");
        ws.Cell(2, 18).GetString().Should().Be("2026-03-01 14:30:00");
        ws.Cell(2, 19).GetString().Should().Be("John Controller");
    }

    [Fact]
    public void AddMselWorksheet_WithoutConductData_DoesNotWriteConductColumns()
    {
        using var workbook = new XLWorkbook();
        var inject = CreateInject(status: InjectStatus.Released);

        MselWorksheetBuilder.AddMselWorksheet(workbook, new List<Inject> { inject }, false, false);

        var ws = workbook.Worksheet("MSEL");
        // Column 17 should be empty — no conduct columns written
        ws.Cell(1, 17).GetString().Should().BeEmpty();
    }

    // =========================================================================
    // Phases Worksheet
    // =========================================================================

    [Fact]
    public void AddPhasesWorksheet_WithPhases_PopulatesAllColumns()
    {
        using var workbook = new XLWorkbook();
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            Sequence = 1,
            Name = "Initial Response",
            Description = "First 30 minutes",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(8, 30),
            OrganizationId = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        MselWorksheetBuilder.AddPhasesWorksheet(workbook, new List<Phase> { phase }, false);

        var ws = workbook.Worksheet("Phases");
        ws.Should().NotBeNull();
        ws.Cell(1, 1).GetString().Should().Be("Sequence");
        ws.Cell(2, 1).GetString().Should().Be("1");
        ws.Cell(2, 2).GetString().Should().Be("Initial Response");
        ws.Cell(2, 3).GetString().Should().Be("First 30 minutes");
        ws.Cell(2, 4).GetString().Should().Be("08:00");
        ws.Cell(2, 5).GetString().Should().Be("08:30");
    }

    [Fact]
    public void AddPhasesWorksheet_WithFormatting_AppliesLightGreenHeader()
    {
        using var workbook = new XLWorkbook();
        var phase = CreatePhase();

        MselWorksheetBuilder.AddPhasesWorksheet(workbook, new List<Phase> { phase }, true);

        var ws = workbook.Worksheet("Phases");
        ws.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        ws.Cell(1, 1).Style.Fill.BackgroundColor.Should().Be(XLColor.LightGreen);
    }

    // =========================================================================
    // Objectives Worksheet
    // =========================================================================

    [Fact]
    public void AddObjectivesWorksheet_WithObjectives_PopulatesAllColumns()
    {
        using var workbook = new XLWorkbook();
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "OBJ-1",
            Name = "EOC Activation",
            Description = "Activate EOC within 30 minutes",
            OrganizationId = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        MselWorksheetBuilder.AddObjectivesWorksheet(workbook, new List<Objective> { objective }, false);

        var ws = workbook.Worksheet("Objectives");
        ws.Should().NotBeNull();
        ws.Cell(1, 1).GetString().Should().Be("Objective #");
        ws.Cell(2, 1).GetString().Should().Be("OBJ-1");
        ws.Cell(2, 2).GetString().Should().Be("EOC Activation");
        ws.Cell(2, 3).GetString().Should().Be("Activate EOC within 30 minutes");
    }

    [Fact]
    public void AddObjectivesWorksheet_WithFormatting_AppliesLightCoralHeader()
    {
        using var workbook = new XLWorkbook();
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ObjectiveNumber = "OBJ-1",
            Name = "EOC Activation",
            OrganizationId = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };

        MselWorksheetBuilder.AddObjectivesWorksheet(workbook, new List<Objective> { objective }, true);

        var ws = workbook.Worksheet("Objectives");
        ws.Cell(1, 1).Style.Font.Bold.Should().BeTrue();
        ws.Cell(1, 1).Style.Fill.BackgroundColor.Should().Be(XLColor.LightCoral);
    }

    // =========================================================================
    // CSV Generation
    // =========================================================================

    [Fact]
    public void GenerateCsv_EmptyList_ReturnsCsvWithHeaderRowOnly()
    {
        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject>(), false);

        csv.Should().NotBeNullOrEmpty();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1); // Header only
        lines[0].Should().Contain("Inject #");
        lines[0].Should().Contain("Title");
        lines[0].Should().Contain("Description");
    }

    [Fact]
    public void GenerateCsv_SingleInject_ContainsCoreFieldValues()
    {
        var inject = CreateInject(
            injectNumber: 7,
            title: "Fire Alarm",
            description: "Fire alarm activated at Building 3",
            scheduledTime: new TimeOnly(11, 15),
            target: "Fire Response Team");

        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject> { inject }, false);

        csv.Should().Contain("7");
        csv.Should().Contain("Fire Alarm");
        csv.Should().Contain("Building 3");
        csv.Should().Contain("11:15");
        csv.Should().Contain("Fire Response Team");
    }

    [Fact]
    public void GenerateCsv_WithConductData_IncludesConductHeaders()
    {
        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject>(), true);

        var headerLine = csv.Split('\n')[0];
        headerLine.Should().Contain("Status");
        headerLine.Should().Contain("Fired At");
        headerLine.Should().Contain("Fired By");
    }

    [Fact]
    public void GenerateCsv_WithConductData_WritesConductValues()
    {
        var firedAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var firedByUser = new ApplicationUser { DisplayName = "Alice Controller" };
        var inject = CreateInject(
            status: InjectStatus.Released,
            firedAt: firedAt,
            firedByUser: firedByUser);

        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject> { inject }, true);

        csv.Should().Contain("Released");
        csv.Should().Contain("2026-03-01 09:00:00");
        csv.Should().Contain("Alice Controller");
    }

    [Fact]
    public void GenerateCsv_WithoutConductData_DoesNotContainStatusHeader()
    {
        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject>(), false);

        var headerLine = csv.Split('\n')[0];
        headerLine.Should().NotContain("Status");
        headerLine.Should().NotContain("Fired At");
    }

    [Fact]
    public void GenerateCsv_FieldWithComma_WrapsInQuotes()
    {
        var inject = CreateInject(description: "Activate EOC, notify personnel");

        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject> { inject }, false);

        csv.Should().Contain("\"Activate EOC, notify personnel\"");
    }

    [Fact]
    public void GenerateCsv_FieldWithQuotes_EscapesDoubledQuotes()
    {
        var inject = CreateInject(description: "Declare a \"major incident\"");

        var csv = MselWorksheetBuilder.GenerateCsv(new List<Inject> { inject }, false);

        csv.Should().Contain("\"Declare a \"\"major incident\"\"\"");
    }
}
