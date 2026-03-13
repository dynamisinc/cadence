using System.Net;
using System.Text.Json;
using Cadence.Core.Data;
using Cadence.Core.Features.Feedback.Models.Enums;
using Cadence.Core.Features.Feedback.Services;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SystemSettingsEntity = Cadence.Core.Features.SystemSettings.Models.Entities.SystemSettings;

namespace Cadence.Core.Tests.Features.Feedback;

/// <summary>
/// Tests for GitHubIssueService — GitHub issue creation, closing, and commenting.
/// </summary>
public class GitHubIssueServiceTests
{
    private readonly Mock<ILogger<GitHubIssueService>> _loggerMock = new();

    private (AppDbContext context, GitHubIssueService service, Mock<HttpMessageHandler> handlerMock) CreateTestContext(
        bool configureGitHub = true,
        bool enableLabels = true)
    {
        var context = TestDbContextFactory.Create();

        if (configureGitHub)
        {
            context.SystemSettings.Add(new SystemSettingsEntity
            {
                Id = Guid.NewGuid(),
                GitHubToken = "ghp_testtoken",
                GitHubOwner = "test-owner",
                GitHubRepo = "test-repo",
                GitHubLabelsEnabled = enableLabels,
                UpdatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("github")).Returns(httpClient);

        var service = new GitHubIssueService(context, factoryMock.Object, _loggerMock.Object);
        return (context, service, handlerMock);
    }

    private void SetupHttpResponse(Mock<HttpMessageHandler> handlerMock, HttpStatusCode statusCode, object? responseBody = null)
    {
        var json = responseBody != null ? JsonSerializer.Serialize(responseBody) : "{}";
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json)
            });
    }

    // =========================================================================
    // CreateIssueAsync
    // =========================================================================

    [Fact]
    public async Task CreateIssueAsync_NoGitHubConfig_ReturnsNull()
    {
        var (_, service, _) = CreateTestContext(configureGitHub: false);

        var result = await service.CreateIssueAsync("REF-001", FeedbackType.BugReport, "Test Bug",
            "High", null, "user@test.com", "Test User", "Test Org");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateIssueAsync_SuccessfulCreation_ReturnsIssueDetails()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.Created, new { number = 42, html_url = "https://github.com/test-owner/test-repo/issues/42" });

        var result = await service.CreateIssueAsync("REF-001", FeedbackType.BugReport, "Test Bug",
            "High", null, "user@test.com", "Test User", "Test Org");

        result.Should().NotBeNull();
        result!.Value.IssueNumber.Should().Be(42);
        result.Value.IssueUrl.Should().Contain("issues/42");
    }

    [Fact]
    public async Task CreateIssueAsync_ApiFailure_ReturnsNull()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.Forbidden, new { message = "Bad credentials" });

        var result = await service.CreateIssueAsync("REF-001", FeedbackType.BugReport, "Test Bug",
            null, null, "user@test.com", null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateIssueAsync_BugReport_IncludesBugLabel()
    {
        var (_, service, handlerMock) = CreateTestContext(enableLabels: true);
        HttpRequestMessage? capturedRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new { number = 1, html_url = "https://github.com/test/issues/1" }))
            });

        await service.CreateIssueAsync("REF-001", FeedbackType.BugReport, "Bug Title",
            null, null, "user@test.com", null, null);

        capturedRequest.Should().NotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        body.Should().Contain("bug");
    }

    [Fact]
    public async Task CreateIssueAsync_FeatureRequest_IncludesEnhancementLabel()
    {
        var (_, service, handlerMock) = CreateTestContext(enableLabels: true);
        HttpRequestMessage? capturedRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new { number = 1, html_url = "https://github.com/test/issues/1" }))
            });

        await service.CreateIssueAsync("REF-002", FeedbackType.FeatureRequest, "Feature Title",
            null, null, "user@test.com", null, null);

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        body.Should().Contain("enhancement");
    }

    [Fact]
    public async Task CreateIssueAsync_LabelsDisabled_NoLabelsInPayload()
    {
        var (_, service, handlerMock) = CreateTestContext(enableLabels: false);
        HttpRequestMessage? capturedRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new { number = 1, html_url = "https://github.com/test/issues/1" }))
            });

        await service.CreateIssueAsync("REF-003", FeedbackType.BugReport, "Bug",
            null, null, "user@test.com", null, null);

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        // Labels array should be empty
        body.Should().Contain("\"labels\":[]");
    }

    [Fact]
    public async Task CreateIssueAsync_WithContentJson_IncludesInBody()
    {
        var (_, service, handlerMock) = CreateTestContext();
        HttpRequestMessage? capturedRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(new { number = 1, html_url = "https://github.com/test/issues/1" }))
            });

        var contentJson = JsonSerializer.Serialize(new { stepsToReproduce = "1. Click button\n2. See error" });
        await service.CreateIssueAsync("REF-004", FeedbackType.BugReport, "Bug Title",
            "Critical", contentJson, "user@test.com", "John Doe", "FEMA");

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        body.Should().Contain("REF-004");
        body.Should().Contain("Bug Report");
    }

    // =========================================================================
    // CloseIssueAsync
    // =========================================================================

    [Fact]
    public async Task CloseIssueAsync_NoGitHubConfig_DoesNotThrow()
    {
        var (_, service, _) = CreateTestContext(configureGitHub: false);

        var act = () => service.CloseIssueAsync(42, "Resolved");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CloseIssueAsync_ValidConfig_SendsPatchRequest()
    {
        var (_, service, handlerMock) = CreateTestContext();
        var requests = new List<HttpRequestMessage>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => requests.Add(req))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        await service.CloseIssueAsync(42, "Fixed");

        // Should have 2 requests: 1 for comment, 1 for close
        requests.Should().HaveCount(2);
        requests.Should().Contain(r => r.Method == HttpMethod.Patch);
    }

    [Fact]
    public async Task CloseIssueAsync_WithoutComment_OnlySendsPatch()
    {
        var (_, service, handlerMock) = CreateTestContext();
        var requests = new List<HttpRequestMessage>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => requests.Add(req))
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        await service.CloseIssueAsync(42);

        requests.Should().HaveCount(1);
        requests[0].Method.Should().Be(HttpMethod.Patch);
    }

    [Fact]
    public async Task CloseIssueAsync_ApiFailure_DoesNotThrow()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.InternalServerError);

        var act = () => service.CloseIssueAsync(42);

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // AddIssueCommentAsync
    // =========================================================================

    [Fact]
    public async Task AddIssueCommentAsync_NoGitHubConfig_DoesNotThrow()
    {
        var (_, service, _) = CreateTestContext(configureGitHub: false);

        var act = () => service.AddIssueCommentAsync(42, "A comment");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddIssueCommentAsync_ValidConfig_SendsPostRequest()
    {
        var (_, service, handlerMock) = CreateTestContext();
        HttpRequestMessage? capturedRequest = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Created });

        await service.AddIssueCommentAsync(42, "Test comment");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("Test comment");
    }

    // =========================================================================
    // TestConnectionAsync
    // =========================================================================

    [Fact]
    public async Task TestConnectionAsync_NoToken_ReturnsFailure()
    {
        var (_, service, _) = CreateTestContext(configureGitHub: false);

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeFalse();
        message.Should().Contain("token");
    }

    [Fact]
    public async Task TestConnectionAsync_SuccessWithPushAccess_ReturnsSuccess()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.OK, new { permissions = new { push = true } });

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeTrue();
        message.Should().Contain("write access");
    }

    [Fact]
    public async Task TestConnectionAsync_SuccessReadOnly_ReturnsSuccessWithWarning()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.OK, new { permissions = new { push = false } });

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeTrue();
        message.Should().Contain("read-only");
    }

    [Fact]
    public async Task TestConnectionAsync_Unauthorized_ReturnsFailure()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.Unauthorized);

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeFalse();
        message.Should().Contain("authentication");
    }

    [Fact]
    public async Task TestConnectionAsync_Forbidden_ReturnsFailure()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.Forbidden);

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeFalse();
        message.Should().Contain("permissions");
    }

    [Fact]
    public async Task TestConnectionAsync_NotFound_ReturnsFailure()
    {
        var (_, service, handlerMock) = CreateTestContext();
        SetupHttpResponse(handlerMock, HttpStatusCode.NotFound);

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeFalse();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task TestConnectionAsync_Exception_ReturnsFailure()
    {
        var (_, service, handlerMock) = CreateTestContext();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var (success, message) = await service.TestConnectionAsync();

        success.Should().BeFalse();
        message.Should().Contain("Connection refused");
    }
}
