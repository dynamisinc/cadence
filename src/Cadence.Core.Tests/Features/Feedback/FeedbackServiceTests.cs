using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models.DTOs;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Features.Feedback.Services;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Feedback;

/// <summary>
/// Unit tests for <see cref="FeedbackService"/>.
/// Covers persisting reports, status updates, soft delete, paginated queries,
/// filtering, sorting, and best-effort GitHub integration (non-critical failures are swallowed).
/// </summary>
public class FeedbackServiceTests
{
    private readonly Mock<IGitHubIssueService> _gitHubMock = new();
    private readonly Mock<ILogger<FeedbackService>> _loggerMock = new();

    // =========================================================================
    // Test Setup Helpers
    // =========================================================================

    private FeedbackService CreateService(AppDbContext context) =>
        new(context, _gitHubMock.Object, _loggerMock.Object);

    private static FeedbackReport CreateReport(
        AppDbContext context,
        string referenceNumber = "CAD-20260101-0001",
        FeedbackType type = FeedbackType.BugReport,
        FeedbackStatus status = FeedbackStatus.New,
        string title = "Test Report",
        string reporterEmail = "tester@example.com",
        int? gitHubIssueNumber = null)
    {
        var report = new FeedbackReport
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = referenceNumber,
            Type = type,
            Status = status,
            Title = title,
            ReporterEmail = reporterEmail,
            GitHubIssueNumber = gitHubIssueNumber,
            GitHubIssueUrl = gitHubIssueNumber.HasValue ? $"https://github.com/org/repo/issues/{gitHubIssueNumber}" : null,
            CreatedBy = "system",
            ModifiedBy = "system"
        };
        context.FeedbackReports.Add(report);
        context.SaveChanges();
        return report;
    }

    // =========================================================================
    // SaveAsync Tests
    // =========================================================================

    [Fact]
    public async Task SaveAsync_ValidSubmission_PersistsReportToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        _gitHubMock.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<FeedbackType>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(((int IssueNumber, string IssueUrl)?)null);

        // Act
        await sut.SaveAsync(
            referenceNumber: "CAD-20260301-0042",
            type: FeedbackType.BugReport,
            reporterEmail: "user@example.com",
            reporterName: "Jane Doe",
            userRole: "OrgAdmin",
            orgName: "City EOC",
            orgRole: "OrgAdmin",
            clientContext: null,
            title: "Clock does not display",
            severity: "High",
            contentJson: null);

        // Assert
        var persisted = context.FeedbackReports.Single();
        persisted.ReferenceNumber.Should().Be("CAD-20260301-0042");
        persisted.Type.Should().Be(FeedbackType.BugReport);
        persisted.Status.Should().Be(FeedbackStatus.New);
        persisted.Title.Should().Be("Clock does not display");
        persisted.ReporterEmail.Should().Be("user@example.com");
        persisted.ReporterName.Should().Be("Jane Doe");
        persisted.Severity.Should().Be("High");
        persisted.OrgName.Should().Be("City EOC");
    }

    [Fact]
    public async Task SaveAsync_WithClientContext_PersistsContextFields()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        var clientContext = new FeedbackClientContext(
            CurrentUrl: "/exercises/123",
            ScreenSize: "1920x1080",
            AppVersion: "1.5.0",
            CommitSha: "abc1234",
            ExerciseId: "exercise-guid-here",
            ExerciseName: "Hurricane TTX 2026",
            ExerciseRole: "Controller");

        _gitHubMock.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<FeedbackType>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(((int IssueNumber, string IssueUrl)?)null);

        // Act
        await sut.SaveAsync("CAD-REF-001", FeedbackType.FeatureRequest,
            "dev@example.com", null, null, null, null, clientContext, "Feature Title", null, null);

        // Assert
        var persisted = context.FeedbackReports.Single();
        persisted.CurrentUrl.Should().Be("/exercises/123");
        persisted.ScreenSize.Should().Be("1920x1080");
        persisted.AppVersion.Should().Be("1.5.0");
        persisted.CommitSha.Should().Be("abc1234");
        persisted.ExerciseId.Should().Be("exercise-guid-here");
        persisted.ExerciseName.Should().Be("Hurricane TTX 2026");
        persisted.ExerciseRole.Should().Be("Controller");
    }

    [Fact]
    public async Task SaveAsync_GitHubSucceeds_StoresIssueNumberAndUrl()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        _gitHubMock.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<FeedbackType>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync((42, "https://github.com/org/repo/issues/42"));

        // Act
        await sut.SaveAsync("CAD-GH-001", FeedbackType.BugReport,
            "reporter@example.com", null, null, null, null, null, "GitHub Test", null, null);

        // Assert
        var persisted = context.FeedbackReports.Single();
        persisted.GitHubIssueNumber.Should().Be(42);
        persisted.GitHubIssueUrl.Should().Be("https://github.com/org/repo/issues/42");
    }

    [Fact]
    public async Task SaveAsync_GitHubThrows_StillPersistsReport()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        _gitHubMock.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<FeedbackType>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("GitHub is unreachable"));

        // Act — must not throw even though GitHub failed
        await sut.SaveAsync("CAD-NOTHROW-001", FeedbackType.General,
            "reporter@example.com", null, null, null, null, null, "Feedback Title", null, null);

        // Assert — report was still saved
        context.FeedbackReports.Should().HaveCount(1);
        context.FeedbackReports.Single().GitHubIssueNumber.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_GitHubReturnsNull_ReportHasNoGitHubFields()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);
        _gitHubMock.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<FeedbackType>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(((int IssueNumber, string IssueUrl)?)null);

        // Act
        await sut.SaveAsync("CAD-NULL-001", FeedbackType.BugReport,
            "reporter@example.com", null, null, null, null, null, "No GitHub", null, null);

        // Assert
        var persisted = context.FeedbackReports.Single();
        persisted.GitHubIssueNumber.Should().BeNull();
        persisted.GitHubIssueUrl.Should().BeNull();
    }

    // =========================================================================
    // UpdateStatusAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateStatusAsync_ValidUpdate_ReturnsUpdatedStatusAndNotes()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, status: FeedbackStatus.New);
        var sut = CreateService(context);

        // Act
        var (status, notes) = await sut.UpdateStatusAsync(report.Id, FeedbackStatus.InReview, "Assigned to dev team");

        // Assert
        status.Should().Be(FeedbackStatus.InReview);
        notes.Should().Be("Assigned to dev team");
    }

    [Fact]
    public async Task UpdateStatusAsync_PersistsChangesToDatabase()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, status: FeedbackStatus.New);
        var sut = CreateService(context);

        // Act
        await sut.UpdateStatusAsync(report.Id, FeedbackStatus.Resolved, "Fixed in v1.6");

        // Assert
        var persisted = await context.FeedbackReports.FindAsync(report.Id);
        persisted!.Status.Should().Be(FeedbackStatus.Resolved);
        persisted.AdminNotes.Should().Be("Fixed in v1.6");
    }

    [Fact]
    public async Task UpdateStatusAsync_ReportNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.UpdateStatusAsync(Guid.NewGuid(), FeedbackStatus.Closed, null);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_NullAdminNotes_ClearsExistingNotes()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, status: FeedbackStatus.InReview);
        report.AdminNotes = "Old notes";
        context.SaveChanges();
        var sut = CreateService(context);

        // Act
        var (_, notes) = await sut.UpdateStatusAsync(report.Id, FeedbackStatus.Resolved, null);

        // Assert
        notes.Should().BeNull();
        var persisted = await context.FeedbackReports.FindAsync(report.Id);
        persisted!.AdminNotes.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_WithGitHubIssue_WhenStatusBecomesClosedCallsCloseIssue()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, gitHubIssueNumber: 99, status: FeedbackStatus.InReview);
        _gitHubMock.Setup(g => g.CloseIssueAsync(99, It.IsAny<string?>())).Returns(Task.CompletedTask);
        var sut = CreateService(context);

        // Act
        await sut.UpdateStatusAsync(report.Id, FeedbackStatus.Closed, null);

        // Assert
        _gitHubMock.Verify(g => g.CloseIssueAsync(99, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithGitHubIssue_WhenAdminNotesChanged_AddsComment()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, gitHubIssueNumber: 55, status: FeedbackStatus.New);
        _gitHubMock.Setup(g => g.AddIssueCommentAsync(55, It.IsAny<string>())).Returns(Task.CompletedTask);
        var sut = CreateService(context);

        // Act
        await sut.UpdateStatusAsync(report.Id, FeedbackStatus.InReview, "Investigating now");

        // Assert
        _gitHubMock.Verify(g => g.AddIssueCommentAsync(55, It.Is<string>(c => c.Contains("Investigating now"))), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_GitHubSyncThrows_DoesNotPropagateException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, gitHubIssueNumber: 77, status: FeedbackStatus.InReview);
        _gitHubMock.Setup(g => g.CloseIssueAsync(77, It.IsAny<string?>()))
            .ThrowsAsync(new Exception("GitHub down"));
        var sut = CreateService(context);

        // Act — closing with GitHub failure should still complete without throwing
        var act = () => sut.UpdateStatusAsync(report.Id, FeedbackStatus.Closed, null);

        // Assert
        await act.Should().NotThrowAsync();
        var persisted = await context.FeedbackReports.FindAsync(report.Id);
        persisted!.Status.Should().Be(FeedbackStatus.Closed);
    }

    // =========================================================================
    // SoftDeleteAsync Tests
    // =========================================================================

    [Fact]
    public async Task SoftDeleteAsync_ReportExists_MarksAsDeleted()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context);
        var sut = CreateService(context);

        // Act
        await sut.SoftDeleteAsync(report.Id, "admin-user-id");

        // Assert
        var persisted = context.FeedbackReports
            .IgnoreQueryFilters()
            .Single(r => r.Id == report.Id);
        persisted.IsDeleted.Should().BeTrue();
        persisted.DeletedAt.Should().NotBeNull();
        persisted.DeletedBy.Should().Be("admin-user-id");
    }

    [Fact]
    public async Task SoftDeleteAsync_ReportNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var act = () => sut.SoftDeleteAsync(Guid.NewGuid(), "admin-user-id");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SoftDeleteAsync_DeletedReportIsHiddenFromSubsequentQueries()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, referenceNumber: "CAD-DEL-001");
        var sut = CreateService(context);

        // Act
        await sut.SoftDeleteAsync(report.Id, "admin");
        var result = await sut.GetReportsAsync();

        // Assert
        result.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task SoftDeleteAsync_WithGitHubIssue_CallsCloseIssue()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, gitHubIssueNumber: 33);
        _gitHubMock.Setup(g => g.CloseIssueAsync(33, It.IsAny<string?>())).Returns(Task.CompletedTask);
        var sut = CreateService(context);

        // Act
        await sut.SoftDeleteAsync(report.Id, "admin");

        // Assert
        _gitHubMock.Verify(g => g.CloseIssueAsync(33, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAsync_GitHubCloseThrows_DoesNotPropagateException()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, gitHubIssueNumber: 44);
        _gitHubMock.Setup(g => g.CloseIssueAsync(44, It.IsAny<string?>()))
            .ThrowsAsync(new Exception("GitHub unreachable"));
        var sut = CreateService(context);

        // Act — GitHub failure during delete must not throw
        var act = () => sut.SoftDeleteAsync(report.Id, "admin");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // GetReportsAsync Tests — Basic Pagination
    // =========================================================================

    [Fact]
    public async Task GetReportsAsync_NoReports_ReturnsEmptyPage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync();

        // Assert
        result.Reports.Should().BeEmpty();
        result.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetReportsAsync_DefaultPagination_ReturnsFirstPage()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        for (int i = 0; i < 5; i++)
            CreateReport(context, referenceNumber: $"CAD-REF-{i:000}",
                title: $"Report {i}", reporterEmail: $"user{i}@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync();

        // Assert
        result.Reports.Should().HaveCount(5);
        result.Pagination.TotalCount.Should().Be(5);
        result.Pagination.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetReportsAsync_PageSizeOf2_ReturnsCorrectPageAndTotalPages()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        for (int i = 0; i < 5; i++)
            CreateReport(context, referenceNumber: $"CAD-P-{i:000}",
                title: $"Report {i}", reporterEmail: $"u{i}@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(page: 1, pageSize: 2);

        // Assert
        result.Reports.Should().HaveCount(2);
        result.Pagination.TotalCount.Should().Be(5);
        result.Pagination.TotalPages.Should().Be(3);
        result.Pagination.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetReportsAsync_SecondPage_ReturnsCorrectSlice()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        for (int i = 0; i < 5; i++)
            CreateReport(context, referenceNumber: $"CAD-S-{i:000}",
                title: $"Report {i}", reporterEmail: $"u{i}@example.com");
        var sut = CreateService(context);

        // Act
        var page1 = await sut.GetReportsAsync(page: 1, pageSize: 3);
        var page2 = await sut.GetReportsAsync(page: 2, pageSize: 3);

        // Assert
        page1.Reports.Should().HaveCount(3);
        page2.Reports.Should().HaveCount(2);
        page1.Reports.Select(r => r.Id).Should().NotIntersectWith(page2.Reports.Select(r => r.Id));
    }

    [Fact]
    public async Task GetReportsAsync_PageLessThanOne_ClampedToOne()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context);
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(page: -5);

        // Assert
        result.Pagination.Page.Should().Be(1);
        result.Reports.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetReportsAsync_PageSizeGreaterThan100_ClampedTo100()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(pageSize: 9999);

        // Assert
        result.Pagination.PageSize.Should().Be(100);
    }

    // =========================================================================
    // GetReportsAsync Tests — Filtering
    // =========================================================================

    [Fact]
    public async Task GetReportsAsync_FilterByType_ReturnsOnlyMatchingType()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "BUG-001", type: FeedbackType.BugReport,
            title: "Bug", reporterEmail: "a@example.com");
        CreateReport(context, referenceNumber: "FEAT-001", type: FeedbackType.FeatureRequest,
            title: "Feature", reporterEmail: "b@example.com");
        CreateReport(context, referenceNumber: "GEN-001", type: FeedbackType.General,
            title: "General", reporterEmail: "c@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(type: FeedbackType.BugReport);

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().Type.Should().Be(FeedbackType.BugReport);
    }

    [Fact]
    public async Task GetReportsAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "NEW-001", status: FeedbackStatus.New,
            title: "New Report", reporterEmail: "a@example.com");
        CreateReport(context, referenceNumber: "CLOSED-001", status: FeedbackStatus.Closed,
            title: "Closed Report", reporterEmail: "b@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(status: FeedbackStatus.New);

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().Status.Should().Be(FeedbackStatus.New);
    }

    [Fact]
    public async Task GetReportsAsync_SearchByTitle_ReturnsMatchingReports()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "A-001", title: "Clock display bug",
            reporterEmail: "a@example.com");
        CreateReport(context, referenceNumber: "A-002", title: "Unrelated issue",
            reporterEmail: "b@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(search: "Clock");

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().Title.Should().Be("Clock display bug");
    }

    [Fact]
    public async Task GetReportsAsync_SearchByReferenceNumber_ReturnsMatchingReports()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "CAD-20260101-FIND",
            title: "Findable", reporterEmail: "a@example.com");
        CreateReport(context, referenceNumber: "CAD-20260101-OTHER",
            title: "Other", reporterEmail: "b@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(search: "FIND");

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().ReferenceNumber.Should().Contain("FIND");
    }

    [Fact]
    public async Task GetReportsAsync_SearchByEmail_ReturnsMatchingReports()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "R-001", title: "Report",
            reporterEmail: "specific@org.com");
        CreateReport(context, referenceNumber: "R-002", title: "Other",
            reporterEmail: "other@org.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(search: "specific@org");

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().ReporterEmail.Should().Be("specific@org.com");
    }

    [Fact]
    public async Task GetReportsAsync_WhitespaceOnlySearch_ReturnsAllReports()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, referenceNumber: "R-001", title: "Alpha",
            reporterEmail: "a@example.com");
        CreateReport(context, referenceNumber: "R-002", title: "Beta",
            reporterEmail: "b@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(search: "   ");

        // Assert
        result.Reports.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReportsAsync_CombinedTypeAndStatusFilter_ReturnsOnlyMatchingBoth()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, "BUG-NEW", FeedbackType.BugReport, FeedbackStatus.New,
            "Bug New", "a@example.com");
        CreateReport(context, "BUG-CLOSED", FeedbackType.BugReport, FeedbackStatus.Closed,
            "Bug Closed", "b@example.com");
        CreateReport(context, "FEAT-NEW", FeedbackType.FeatureRequest, FeedbackStatus.New,
            "Feat New", "c@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(type: FeedbackType.BugReport, status: FeedbackStatus.New);

        // Assert
        result.Reports.Should().HaveCount(1);
        result.Reports.Single().ReferenceNumber.Should().Be("BUG-NEW");
    }

    // =========================================================================
    // GetReportsAsync Tests — Sorting
    // =========================================================================

    [Fact]
    public async Task GetReportsAsync_DefaultSort_OrdersByCreatedAtDescending()
    {
        // Arrange — use named DB so ordering is deterministic
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);

        // Manually set CreatedAt to control order
        var older = new FeedbackReport
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "OLD-001",
            Type = FeedbackType.General,
            Status = FeedbackStatus.New,
            Title = "Older Report",
            ReporterEmail = "old@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            CreatedBy = "system",
            ModifiedBy = "system"
        };
        var newer = new FeedbackReport
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "NEW-001",
            Type = FeedbackType.General,
            Status = FeedbackStatus.New,
            Title = "Newer Report",
            ReporterEmail = "new@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "system",
            ModifiedBy = "system"
        };
        context.FeedbackReports.AddRange(older, newer);
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(sortDesc: true);

        // Assert — newest first
        result.Reports[0].ReferenceNumber.Should().Be("NEW-001");
        result.Reports[1].ReferenceNumber.Should().Be("OLD-001");
    }

    [Fact]
    public async Task GetReportsAsync_SortByTitleAscending_ReturnsTitleAlphabetically()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, "R-C", title: "Charlie", reporterEmail: "c@example.com");
        CreateReport(context, "R-A", title: "Alpha", reporterEmail: "a@example.com");
        CreateReport(context, "R-B", title: "Bravo", reporterEmail: "b@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(sortBy: "title", sortDesc: false);

        // Assert
        result.Reports[0].Title.Should().Be("Alpha");
        result.Reports[1].Title.Should().Be("Bravo");
        result.Reports[2].Title.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetReportsAsync_SortByTitleDescending_ReturnsTitleReverseAlphabetically()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, "R-C", title: "Charlie", reporterEmail: "c@example.com");
        CreateReport(context, "R-A", title: "Alpha", reporterEmail: "a@example.com");
        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync(sortBy: "title", sortDesc: true);

        // Assert
        result.Reports[0].Title.Should().Be("Charlie");
        result.Reports[1].Title.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetReportsAsync_UnknownSortBy_FallsBackToCreatedAt()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        CreateReport(context, "R-001", title: "One", reporterEmail: "a@example.com");
        var sut = CreateService(context);

        // Act — should not throw for unrecognised sort key
        var act = () => sut.GetReportsAsync(sortBy: "nonexistent_field");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // GetReportsAsync Tests — Dto Mapping
    // =========================================================================

    [Fact]
    public async Task GetReportsAsync_ReturnsMappedDtoFields()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var report = CreateReport(context, "CAD-MAP-001", FeedbackType.FeatureRequest,
            FeedbackStatus.InReview, "Feature Title", "mapper@example.com");
        report.Severity = "Low";
        report.OrgName = "Test Org";
        context.SaveChanges();

        var sut = CreateService(context);

        // Act
        var result = await sut.GetReportsAsync();

        // Assert
        var dto = result.Reports.Single();
        dto.Id.Should().Be(report.Id);
        dto.ReferenceNumber.Should().Be("CAD-MAP-001");
        dto.Type.Should().Be(FeedbackType.FeatureRequest);
        dto.Status.Should().Be(FeedbackStatus.InReview);
        dto.Title.Should().Be("Feature Title");
        dto.ReporterEmail.Should().Be("mapper@example.com");
        dto.Severity.Should().Be("Low");
        dto.OrgName.Should().Be("Test Org");
    }
}
