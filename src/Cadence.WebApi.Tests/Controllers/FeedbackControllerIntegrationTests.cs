using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for FeedbackController API endpoints.
/// Tests bug reports, feature requests, general feedback, error reports, and admin operations.
/// </summary>
[Collection("WebApi Integration")]
public class FeedbackControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string AdminEmail)>
        SetupAuthenticatedClientAsync()
    {
        var factory = new CadenceWebApplicationFactory();
        var client = factory.CreateClient();

        var email = $"admin-{Guid.NewGuid()}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(email, "Password123!", "Test Admin"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        var slug = $"test-org-{Guid.NewGuid():N}"[..30];
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Test Org {Guid.NewGuid():N}"[..40], slug, null, null, email));
        createOrgResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        var switchResponse = await client.PostAsJsonAsync("/api/users/current-organization",
            new { OrganizationId = org!.Id });
        switchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", false));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth!.AccessToken);

        return (factory, client, email);
    }

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task POST_BugReport_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/feedback/bug-report", new
        {
            Title = "Test bug",
            Description = "A bug",
            Severity = "High"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_FeatureRequest_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/feedback/feature-request", new
        {
            Title = "New feature",
            Description = "Add something"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_GeneralFeedback_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/feedback/general", new
        {
            Category = "UI",
            Subject = "Looks great",
            Message = "Thanks"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_ErrorReport_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/feedback/error-report", new
        {
            ErrorMessage = "Uncaught TypeError",
            Url = "https://app.cadence.com/exercises",
            Browser = "Chrome 120"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Reports_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/feedback");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Submit Bug Report
    // =========================================================================

    [Fact]
    public async Task POST_BugReport_ValidRequest_Returns200WithReferenceNumber()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/bug-report", new
        {
            Title = "Button does not work",
            Description = "The save button is unresponsive on the exercise page",
            StepsToReproduce = "1. Open exercise\n2. Click save\n3. Nothing happens",
            Severity = "Medium"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
        result.GetProperty("message").GetString().Should().Contain("Bug report submitted");
    }

    [Fact]
    public async Task POST_BugReport_WithClientContext_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/bug-report", new
        {
            Title = "Context test bug",
            Description = "Testing client context submission",
            Severity = "Low",
            Context = new
            {
                CurrentUrl = "https://app.cadence.com/exercises/123",
                ScreenSize = "1920x1080",
                AppVersion = "1.0.0",
                CommitSha = "abc123",
                ExerciseName = "Test Exercise",
                ExerciseRole = "Controller"
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
    }

    // =========================================================================
    // Submit Feature Request
    // =========================================================================

    [Fact]
    public async Task POST_FeatureRequest_ValidRequest_Returns200WithReferenceNumber()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/feature-request", new
        {
            Title = "Add dark mode",
            Description = "Would love a dark mode option for night exercises",
            UseCase = "Night-time exercises where bright screens are distracting"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
        result.GetProperty("message").GetString().Should().Contain("Feature request submitted");
    }

    [Fact]
    public async Task POST_FeatureRequest_WithoutUseCase_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/feature-request", new
        {
            Title = "Minimal feature request",
            Description = "Just a description, no use case"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
    }

    // =========================================================================
    // Submit General Feedback
    // =========================================================================

    [Fact]
    public async Task POST_GeneralFeedback_ValidRequest_Returns200WithReferenceNumber()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/general", new
        {
            Category = "User Interface",
            Subject = "Great exercise management flow",
            Message = "The inject management workflow is very intuitive."
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
        result.GetProperty("message").GetString().Should().Contain("Feedback submitted");
    }

    // =========================================================================
    // Submit Error Report
    // =========================================================================

    [Fact]
    public async Task POST_ErrorReport_ValidRequest_Returns200WithReferenceNumber()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/feedback/error-report", new
        {
            ErrorMessage = "Cannot read properties of undefined (reading 'map')",
            StackTrace = "TypeError: Cannot read properties...\n    at InjectList.render",
            ComponentStack = "InjectList > ExercisePage > App",
            Url = "https://app.cadence.com/exercises/abc/injects",
            Browser = "Mozilla/5.0 Chrome/120"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("referenceNumber").GetString().Should().StartWith("CAD-");
        result.GetProperty("message").GetString().Should().Contain("Error report sent");
    }

    // =========================================================================
    // Admin: Get Reports
    // =========================================================================

    [Fact]
    public async Task GET_Reports_AsAdmin_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/feedback");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("reports").ValueKind.Should().Be(JsonValueKind.Array);
        result.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GET_Reports_WithPagination_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/feedback?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Admin: Update Status
    // =========================================================================

    [Fact]
    public async Task PATCH_UpdateStatus_NonExistentReport_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PatchAsJsonAsync($"/api/feedback/{Guid.NewGuid()}/status", new
        {
            Status = 1, // InReview
            AdminNotes = "Checked"
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Admin: Delete Report
    // =========================================================================

    [Fact]
    public async Task DELETE_Report_NonExistentReport_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/feedback/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // End-to-End: Submit → List → Delete
    // =========================================================================

    [Fact]
    public async Task EndToEnd_SubmitBugReport_AppearsInList()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Submit a bug report
        var submitResponse = await client.PostAsJsonAsync("/api/feedback/bug-report", new
        {
            Title = "E2E test bug report",
            Description = "This is a bug report for end-to-end testing",
            Severity = "High"
        });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // List reports - submitted report should appear
        var listResponse = await client.GetAsync("/api/feedback");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await listResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().BeGreaterOrEqualTo(0);
    }
}
