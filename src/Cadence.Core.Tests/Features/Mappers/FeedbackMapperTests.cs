using Cadence.Core.Features.Feedback.Mappers;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Enums;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Mappers;

public class FeedbackMapperTests
{
    private static FeedbackReport CreateTestReport() => new()
    {
        Id = Guid.NewGuid(),
        ReferenceNumber = "CAD-20260315-0001",
        Type = FeedbackType.BugReport,
        Status = FeedbackStatus.New,
        Title = "Login button unresponsive",
        Severity = "High",
        ContentJson = """{"steps":"Click login","expected":"Navigate to dashboard"}""",
        ReporterEmail = "evaluator@example.com",
        ReporterName = "Jane Smith",
        UserRole = "User",
        OrgName = "Metro Fire",
        OrgRole = "OrgUser",
        CurrentUrl = "/exercises/123",
        ScreenSize = "1920x1080",
        AppVersion = "1.2.0",
        CommitSha = "abc123def",
        ExerciseId = "exercise-guid",
        ExerciseName = "Spring TTX 2026",
        ExerciseRole = "Evaluator",
        AdminNotes = "Investigating",
        GitHubIssueNumber = 42,
        GitHubIssueUrl = "https://github.com/org/cadence/issues/42",
        CreatedAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2026, 3, 15, 11, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var entity = CreateTestReport();

        var dto = entity.ToDto();

        dto.Id.Should().Be(entity.Id);
        dto.ReferenceNumber.Should().Be("CAD-20260315-0001");
        dto.Type.Should().Be(FeedbackType.BugReport);
        dto.Status.Should().Be(FeedbackStatus.New);
        dto.Title.Should().Be("Login button unresponsive");
        dto.Severity.Should().Be("High");
        dto.ContentJson.Should().Contain("steps");
        dto.ReporterEmail.Should().Be("evaluator@example.com");
        dto.ReporterName.Should().Be("Jane Smith");
        dto.UserRole.Should().Be("User");
        dto.OrgName.Should().Be("Metro Fire");
        dto.OrgRole.Should().Be("OrgUser");
        dto.CurrentUrl.Should().Be("/exercises/123");
        dto.ScreenSize.Should().Be("1920x1080");
        dto.AppVersion.Should().Be("1.2.0");
        dto.CommitSha.Should().Be("abc123def");
        dto.ExerciseId.Should().Be("exercise-guid");
        dto.ExerciseName.Should().Be("Spring TTX 2026");
        dto.ExerciseRole.Should().Be("Evaluator");
        dto.AdminNotes.Should().Be("Investigating");
        dto.GitHubIssueNumber.Should().Be(42);
        dto.GitHubIssueUrl.Should().Be("https://github.com/org/cadence/issues/42");
        dto.CreatedAt.Should().Be(entity.CreatedAt);
        dto.UpdatedAt.Should().Be(entity.UpdatedAt);
    }

    [Fact]
    public void ToDto_NullOptionalFields_MapsAsNull()
    {
        var entity = CreateTestReport();
        entity.Severity = null;
        entity.ContentJson = null;
        entity.ReporterName = null;
        entity.UserRole = null;
        entity.OrgName = null;
        entity.OrgRole = null;
        entity.CurrentUrl = null;
        entity.ScreenSize = null;
        entity.AppVersion = null;
        entity.CommitSha = null;
        entity.ExerciseId = null;
        entity.ExerciseName = null;
        entity.ExerciseRole = null;
        entity.AdminNotes = null;
        entity.GitHubIssueNumber = null;
        entity.GitHubIssueUrl = null;

        var dto = entity.ToDto();

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
    public void ToDto_FeatureRequest_MapsType()
    {
        var entity = CreateTestReport();
        entity.Type = FeedbackType.FeatureRequest;
        entity.Severity = null;

        var dto = entity.ToDto();

        dto.Type.Should().Be(FeedbackType.FeatureRequest);
    }
}
