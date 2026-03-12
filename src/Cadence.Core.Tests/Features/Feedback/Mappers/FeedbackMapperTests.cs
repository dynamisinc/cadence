using Cadence.Core.Features.Feedback.Mappers;
using Cadence.Core.Features.Feedback.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Enums;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Feedback.Mappers;

/// <summary>
/// Unit tests for FeedbackMapper extension methods.
/// Verifies correct projection of FeedbackReport entities to FeedbackReportDto.
/// </summary>
public class FeedbackMapperTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static FeedbackReport BuildFullReport() => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = "CAD-20260305-0042",
        Type = FeedbackType.BugReport,
        Status = FeedbackStatus.InReview,
        Title = "Clock does not advance after exercise resumes",
        Severity = "High",
        ContentJson = """{"stepsToReproduce":"1. Start clock\n2. Pause\n3. Resume"}""",
        ReporterEmail = "reporter@example.gov",
        ReporterName = "Alice Controller",
        UserRole = "Manager",
        OrgName = "Acme Emergency Services",
        OrgRole = "OrgAdmin",
        CurrentUrl = "https://app.example.com/exercises/abc123/conduct",
        ScreenSize = "1920x1080",
        AppVersion = "1.5.0",
        CommitSha = "abc1234",
        ExerciseId = "ex-guid-string",
        ExerciseName = "Hurricane Response TTX",
        ExerciseRole = "Controller",
        AdminNotes = "Reproduced in staging. Priority fix.",
        GitHubIssueNumber = 177,
        GitHubIssueUrl = "https://github.com/org/cadence/issues/177",
        CreatedAt = new DateTime(2026, 3, 5, 10, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 3, 6, 14, 30, 0, DateTimeKind.Utc)
    };

    // =========================================================================
    // ToDto — full mapping
    // =========================================================================

    [Fact]
    public void ToDto_AllPropertiesPopulated_MapsEveryField()
    {
        // Arrange
        FeedbackReport entity = BuildFullReport();

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.ReferenceNumber.Should().Be(entity.ReferenceNumber);
        dto.Type.Should().Be(entity.Type);
        dto.Status.Should().Be(entity.Status);
        dto.Title.Should().Be(entity.Title);
        dto.Severity.Should().Be(entity.Severity);
        dto.ContentJson.Should().Be(entity.ContentJson);
        dto.ReporterEmail.Should().Be(entity.ReporterEmail);
        dto.ReporterName.Should().Be(entity.ReporterName);
        dto.UserRole.Should().Be(entity.UserRole);
        dto.OrgName.Should().Be(entity.OrgName);
        dto.OrgRole.Should().Be(entity.OrgRole);
        dto.CurrentUrl.Should().Be(entity.CurrentUrl);
        dto.ScreenSize.Should().Be(entity.ScreenSize);
        dto.AppVersion.Should().Be(entity.AppVersion);
        dto.CommitSha.Should().Be(entity.CommitSha);
        dto.ExerciseId.Should().Be(entity.ExerciseId);
        dto.ExerciseName.Should().Be(entity.ExerciseName);
        dto.ExerciseRole.Should().Be(entity.ExerciseRole);
        dto.AdminNotes.Should().Be(entity.AdminNotes);
        dto.GitHubIssueNumber.Should().Be(entity.GitHubIssueNumber);
        dto.GitHubIssueUrl.Should().Be(entity.GitHubIssueUrl);
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    // =========================================================================
    // ToDto — FeedbackType enum variants
    // =========================================================================

    [Theory]
    [InlineData(FeedbackType.BugReport)]
    [InlineData(FeedbackType.FeatureRequest)]
    [InlineData(FeedbackType.General)]
    public void ToDto_AllFeedbackTypes_MapsTypeCorrectly(FeedbackType type)
    {
        // Arrange
        var entity = BuildFullReport();
        entity.Type = type;

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.Type.Should().Be(type);
    }

    // =========================================================================
    // ToDto — FeedbackStatus enum variants
    // =========================================================================

    [Theory]
    [InlineData(FeedbackStatus.New)]
    [InlineData(FeedbackStatus.InReview)]
    [InlineData(FeedbackStatus.Resolved)]
    [InlineData(FeedbackStatus.Closed)]
    public void ToDto_AllFeedbackStatuses_MapsStatusCorrectly(FeedbackStatus status)
    {
        // Arrange
        var entity = BuildFullReport();
        entity.Status = status;

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.Status.Should().Be(status);
    }

    // =========================================================================
    // ToDto — nullable fields
    // =========================================================================

    [Fact]
    public void ToDto_AllNullablePropertiesAreNull_MapsNullsThrough()
    {
        // Arrange
        var entity = new FeedbackReport
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "CAD-20260305-0001",
            Type = FeedbackType.General,
            Status = FeedbackStatus.New,
            Title = "General feedback",
            Severity = null,
            ContentJson = null,
            ReporterEmail = "user@example.com",
            ReporterName = null,
            UserRole = null,
            OrgName = null,
            OrgRole = null,
            CurrentUrl = null,
            ScreenSize = null,
            AppVersion = null,
            CommitSha = null,
            ExerciseId = null,
            ExerciseName = null,
            ExerciseRole = null,
            AdminNotes = null,
            GitHubIssueNumber = null,
            GitHubIssueUrl = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.Severity.Should().BeNull();
        dto.ContentJson.Should().BeNull();
        dto.ReporterName.Should().BeNull();
        dto.UserRole.Should().BeNull();
        dto.OrgName.Should().BeNull();
        dto.OrgRole.Should().BeNull();
        dto.CurrentUrl.Should().BeNull();
        dto.ScreenSize.Should().BeNull();
        dto.AppVersion.Should().BeNull();
        dto.CommitSha.Should().BeNull();
        dto.ExerciseId.Should().BeNull();
        dto.ExerciseName.Should().BeNull();
        dto.ExerciseRole.Should().BeNull();
        dto.AdminNotes.Should().BeNull();
        dto.GitHubIssueNumber.Should().BeNull();
        dto.GitHubIssueUrl.Should().BeNull();
    }

    [Fact]
    public void ToDto_GitHubIssueNumberPresent_MapsIssueNumber()
    {
        // Arrange
        var entity = BuildFullReport();
        entity.GitHubIssueNumber = 999;
        entity.GitHubIssueUrl = "https://github.com/org/repo/issues/999";

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.GitHubIssueNumber.Should().Be(999);
        dto.GitHubIssueUrl.Should().Be("https://github.com/org/repo/issues/999");
    }

    [Fact]
    public void ToDto_GitHubIssueNumberNull_MapsNullIssueNumber()
    {
        // Arrange
        var entity = BuildFullReport();
        entity.GitHubIssueNumber = null;
        entity.GitHubIssueUrl = null;

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.GitHubIssueNumber.Should().BeNull();
        dto.GitHubIssueUrl.Should().BeNull();
    }

    // =========================================================================
    // ToDto — required string fields
    // =========================================================================

    [Fact]
    public void ToDto_RequiredStringFieldsPresent_MapsStringsCorrectly()
    {
        // Arrange
        var entity = BuildFullReport();
        entity.ReferenceNumber = "CAD-20260401-0100";
        entity.ReporterEmail = "test@test.gov";
        entity.Title = "Important bug";

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.ReferenceNumber.Should().Be("CAD-20260401-0100");
        dto.ReporterEmail.Should().Be("test@test.gov");
        dto.Title.Should().Be("Important bug");
    }

    // =========================================================================
    // ToDto — timestamps
    // =========================================================================

    [Fact]
    public void ToDto_TimestampsPreserved_MapsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc);

        var entity = BuildFullReport();
        entity.CreatedAt = createdAt;
        entity.UpdatedAt = updatedAt;

        // Act
        FeedbackReportDto dto = entity.ToDto();

        // Assert
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }
}
