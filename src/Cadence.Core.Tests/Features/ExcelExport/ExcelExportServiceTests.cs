using Cadence.Core.Data;
using Cadence.Core.Features.ExcelExport.Models.DTOs;
using Cadence.Core.Features.ExcelExport.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.ExcelExport;

public class ExcelExportServiceTests
{
    private readonly Mock<ILogger<ExcelExportService>> _loggerMock;

    public ExcelExportServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExcelExportService>>();
    }

    private ExcelExportService CreateService(AppDbContext context)
    {
        return new ExcelExportService(context, _loggerMock.Object);
    }

    private (AppDbContext context, Organization org, Exercise exercise, Msel msel) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Test Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);

        var msel = new Msel
        {
            Id = Guid.NewGuid(),
            Name = "Test MSEL",
            Version = 1,
            IsActive = true,
            ExerciseId = exercise.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Msels.Add(msel);

        context.SaveChanges();

        return (context, org, exercise, msel);
    }

    private Inject CreateInject(AppDbContext context, Msel msel, int injectNumber, string title)
    {
        var inject = new Inject
        {
            Id = Guid.NewGuid(),
            InjectNumber = injectNumber,
            Title = title,
            Description = $"Description for {title}",
            ScheduledTime = new TimeOnly(9, 0).Add(TimeSpan.FromMinutes(injectNumber * 15)),
            Target = "EOC",
            InjectType = InjectType.Standard,
            Status = InjectStatus.Pending,
            Sequence = injectNumber,
            MselId = msel.Id,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Injects.Add(inject);
        context.SaveChanges();

        return inject;
    }

    private Phase CreatePhase(AppDbContext context, Exercise exercise, int sequence, string name)
    {
        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            Name = name,
            Description = $"Description for {name}",
            Sequence = sequence,
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Phases.Add(phase);
        context.SaveChanges();

        return phase;
    }

    private Objective CreateObjective(AppDbContext context, Exercise exercise, string objectiveNumber, string name)
    {
        var objective = new Objective
        {
            Id = Guid.NewGuid(),
            ExerciseId = exercise.Id,
            ObjectiveNumber = objectiveNumber,
            Name = name,
            Description = $"Description for {name}",
            CreatedBy = Guid.Empty.ToString(),
            ModifiedBy = Guid.Empty.ToString()
        };
        context.Objectives.Add(objective);
        context.SaveChanges();

        return objective;
    }

    #region ExportMselAsync Tests

    [Fact]
    public async Task ExportMselAsync_ValidExercise_ReturnsExcelFile()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 1, "Test Inject 1");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Filename.Should().Contain("MSEL");
        result.Filename.Should().EndWith(".xlsx");
        result.InjectCount.Should().Be(1);
    }

    [Fact]
    public async Task ExportMselAsync_ExerciseNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await service.ExportMselAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task ExportMselAsync_NoActiveMsel_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Org",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "No MSEL Exercise",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
            TimeZoneId = "UTC",
            OrganizationId = org.Id,
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Exercises.Add(exercise);
        context.SaveChanges();

        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id
        };

        // Act
        Func<Task> act = async () => await service.ExportMselAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*MSEL*");
    }

    [Fact]
    public async Task ExportMselAsync_WithInjects_IncludesAllInjectsInOrder()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 3, "Third Inject");
        CreateInject(context, msel, 1, "First Inject");
        CreateInject(context, msel, 2, "Second Inject");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.InjectCount.Should().Be(3);

        // Verify content using ClosedXML
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Data starts at row 2 (row 1 is header)
        // Injects should be ordered by sequence
        worksheet.Cell(2, 1).GetString().Should().Be("1"); // First inject number
        worksheet.Cell(3, 1).GetString().Should().Be("2"); // Second inject number
        worksheet.Cell(4, 1).GetString().Should().Be("3"); // Third inject number
    }

    [Fact]
    public async Task ExportMselAsync_CsvFormat_ReturnsCsvFile()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 1, "Test Inject");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id,
            Format = "csv"
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("text/csv");
        result.Filename.Should().EndWith(".csv");

        // Verify it's valid CSV content
        var csvContent = System.Text.Encoding.UTF8.GetString(result.Content);
        csvContent.Should().Contain("Inject #");
        csvContent.Should().Contain("Title");
        csvContent.Should().Contain("Test Inject");
    }

    [Fact]
    public async Task ExportMselAsync_IncludeConductData_AddsStatusColumns()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        var inject = CreateInject(context, msel, 1, "Fired Inject");
        inject.Status = InjectStatus.Fired;
        inject.FiredAt = DateTime.UtcNow;
        context.SaveChanges();

        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id,
            IncludeConductData = true
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Find Status column (should be after standard columns)
        var headerRow = worksheet.Row(1);
        var statusColumnIndex = -1;
        for (int i = 1; i <= 20; i++)
        {
            if (headerRow.Cell(i).GetString() == "Status")
            {
                statusColumnIndex = i;
                break;
            }
        }

        statusColumnIndex.Should().BeGreaterThan(0, "Status column should exist");
        worksheet.Cell(2, statusColumnIndex).GetString().Should().Be("Fired");
    }

    [Fact]
    public async Task ExportMselAsync_IncludePhases_AddsPhasesWorksheet()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 1, "Test Inject");
        CreatePhase(context, exercise, 1, "Planning Phase");
        CreatePhase(context, exercise, 2, "Response Phase");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id,
            IncludePhases = true
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.PhaseCount.Should().Be(2);

        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Contains("Phases").Should().BeTrue();
        var phasesSheet = workbook.Worksheet("Phases");
        phasesSheet.Cell(2, 2).GetString().Should().Be("Planning Phase");
        phasesSheet.Cell(3, 2).GetString().Should().Be("Response Phase");
    }

    [Fact]
    public async Task ExportMselAsync_IncludeObjectives_AddsObjectivesWorksheet()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 1, "Test Inject");
        CreateObjective(context, exercise, "1", "Primary Objective");
        CreateObjective(context, exercise, "2", "Secondary Objective");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id,
            IncludeObjectives = true
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.ObjectiveCount.Should().Be(2);

        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Contains("Objectives").Should().BeTrue();
        var objectivesSheet = workbook.Worksheet("Objectives");
        objectivesSheet.Cell(2, 2).GetString().Should().Be("Primary Objective");
    }

    [Fact]
    public async Task ExportMselAsync_CustomFilename_UsesProvidedName()
    {
        // Arrange
        var (context, _, exercise, msel) = CreateTestContext();
        CreateInject(context, msel, 1, "Test Inject");
        var service = CreateService(context);

        var request = new ExportMselRequest
        {
            ExerciseId = exercise.Id,
            Filename = "MyCustomExport"
        };

        // Act
        var result = await service.ExportMselAsync(request);

        // Assert
        result.Filename.Should().StartWith("MyCustomExport");
        result.Filename.Should().EndWith(".xlsx");
    }

    #endregion

    #region GenerateTemplateAsync Tests

    [Fact]
    public async Task GenerateTemplateAsync_ReturnsValidExcelFile()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeEmpty();
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Filename.Should().Contain("Template");
        result.Filename.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task GenerateTemplateAsync_IncludesAllColumnHeaders()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Check for expected headers
        var expectedHeaders = new[] { "Inject #", "Title", "Description", "Scheduled Time" };
        var headerRow = worksheet.Row(1);

        foreach (var header in expectedHeaders)
        {
            var found = false;
            for (int i = 1; i <= 20; i++)
            {
                if (headerRow.Cell(i).GetString() == header)
                {
                    found = true;
                    break;
                }
            }
            found.Should().BeTrue($"Header '{header}' should exist");
        }
    }

    [Fact]
    public async Task GenerateTemplateAsync_IncludesExampleRow()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Row 2 should have example data
        var exampleInjectNumber = worksheet.Cell(2, 1).GetString();
        exampleInjectNumber.Should().Be("1", "Example row should have inject number 1");

        var exampleTitle = worksheet.Cell(2, 2).GetString();
        exampleTitle.Should().NotBeNullOrEmpty("Example row should have a title");
    }

    [Fact]
    public async Task GenerateTemplateAsync_WithFormatting_AppliesStyles()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync(includeFormatting: true);

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Header row should have bold formatting
        var headerCell = worksheet.Cell(1, 1);
        headerCell.Style.Font.Bold.Should().BeTrue("Header should be bold when formatting enabled");
    }

    [Fact]
    public async Task GenerateTemplateAsync_WithoutFormatting_NoStyles()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync(includeFormatting: false);

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("MSEL");

        // Header row should not have bold formatting when disabled
        var headerCell = worksheet.Cell(1, 1);
        headerCell.Style.Font.Bold.Should().BeFalse("Header should not be bold when formatting disabled");
    }

    [Fact]
    public async Task GenerateTemplateAsync_IncludesInstructionsWorksheet()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Contains("Instructions").Should().BeTrue("Template should have Instructions worksheet");

        var instructionsSheet = workbook.Worksheet("Instructions");
        instructionsSheet.Cell(1, 1).GetString().Should().Be("Overview");
        instructionsSheet.Cell(1, 2).GetString().Should().Contain("MSEL");
    }

    [Fact]
    public async Task GenerateTemplateAsync_InstructionsWorksheetIsFirst()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        // Instructions should be the first worksheet (position 1)
        workbook.Worksheet(1).Name.Should().Be("Instructions");
    }

    [Fact]
    public async Task GenerateTemplateAsync_IncludesLookupsWorksheet()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Contains("Lookups").Should().BeTrue("Template should have Lookups worksheet");
    }

    [Fact]
    public async Task GenerateTemplateAsync_LookupsWorksheetContainsDeliveryMethods()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        var lookupsSheet = workbook.Worksheet("Lookups");
        lookupsSheet.Cell(1, 1).GetString().Should().Be("Delivery Methods");

        // Check for expected delivery method values
        var deliveryMethods = new[] { "Verbal", "Phone", "Email", "Radio", "Written", "Simulation", "Other" };
        for (int i = 0; i < deliveryMethods.Length; i++)
        {
            lookupsSheet.Cell(i + 2, 1).GetString().Should().Be(deliveryMethods[i]);
        }
    }

    [Fact]
    public async Task GenerateTemplateAsync_LookupsWorksheetContainsPriorities()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        var lookupsSheet = workbook.Worksheet("Lookups");
        lookupsSheet.Cell(1, 2).GetString().Should().Be("Priority");

        // Check priority values 1-5 (stored as numbers)
        for (int i = 1; i <= 5; i++)
        {
            lookupsSheet.Cell(i + 1, 2).GetValue<int>().Should().Be(i);
        }
    }

    [Fact]
    public async Task GenerateTemplateAsync_MselWorksheetHasDataValidation()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = CreateService(context);

        // Act
        var result = await service.GenerateTemplateAsync();

        // Assert
        using var stream = new MemoryStream(result.Content);
        using var workbook = new XLWorkbook(stream);

        var mselSheet = workbook.Worksheet("MSEL");

        // Find the Delivery Method column (column 9 based on MselColumns array)
        // Check that data validation exists
        var deliveryMethodCell = mselSheet.Cell(2, 9); // Row 2, Delivery Method column
        var validations = mselSheet.DataValidations;
        validations.Should().NotBeEmpty("MSEL worksheet should have data validations");
    }

    #endregion
}
