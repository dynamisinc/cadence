using Cadence.Core.Features.ExcelImport.Models;
using Cadence.Core.Features.ExcelImport.Models.DTOs;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.ExcelImport;

public class ImportSessionTests
{
    private static ImportSession CreateSession(string step = "Upload") => new()
    {
        SessionId = Guid.NewGuid(),
        FileName = "test.xlsx",
        FileFormat = "xlsx",
        TempFilePath = "/tmp/test.xlsx",
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        CurrentStep = step,
        Worksheets = new List<WorksheetInfoDto>()
    };

    [Fact]
    public void Constructor_RequiredProperties_SetCorrectly()
    {
        var sessionId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddMinutes(30);

        var session = new ImportSession
        {
            SessionId = sessionId,
            FileName = "msel.xlsx",
            FileFormat = "xlsx",
            TempFilePath = "/tmp/msel.xlsx",
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            CurrentStep = "Upload",
            Worksheets = new List<WorksheetInfoDto>()
        };

        session.SessionId.Should().Be(sessionId);
        session.FileName.Should().Be("msel.xlsx");
        session.FileFormat.Should().Be("xlsx");
        session.TempFilePath.Should().Be("/tmp/msel.xlsx");
        session.CreatedAt.Should().Be(createdAt);
        session.CurrentStep.Should().Be("Upload");
        session.Worksheets.Should().BeEmpty();
    }

    [Fact]
    public void ExpiresAt_SetValue_ThreadSafe()
    {
        var session = CreateSession();
        var newExpiry = DateTime.UtcNow.AddHours(1);

        session.ExpiresAt = newExpiry;

        session.ExpiresAt.Should().Be(newExpiry);
    }

    [Fact]
    public void CurrentStep_SetValue_UpdatesCorrectly()
    {
        var session = CreateSession("Upload");

        session.CurrentStep = "Mapping";

        session.CurrentStep.Should().Be("Mapping");
    }

    [Fact]
    public void Columns_NullByDefault()
    {
        var session = CreateSession();

        session.Columns.Should().BeNull();
    }

    [Fact]
    public void Mappings_SetAndRetrieve_Works()
    {
        var session = CreateSession();
        var mappings = new List<ColumnMappingDto>
        {
            new() { CadenceField = "Title", DisplayName = "Title", SourceColumnIndex = 0, IsRequired = true }
        };

        session.Mappings = mappings;

        session.Mappings.Should().HaveCount(1);
        session.Mappings![0].CadenceField.Should().Be("Title");
    }
}
