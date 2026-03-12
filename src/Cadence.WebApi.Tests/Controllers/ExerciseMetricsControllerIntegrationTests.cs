using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExerciseMetricsController API endpoints.
/// Tests exercise progress, inject metrics, observation metrics, timeline,
/// controller activity, evaluator coverage, and capability performance endpoints.
/// </summary>
[Collection("WebApi Integration")]
public class ExerciseMetricsControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a fresh factory + authenticated admin client with org context.
    /// Returns disposable factory, client, and admin email.
    /// </summary>
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

    /// <summary>
    /// Creates an exercise via POST and returns the exercise ID string.
    /// </summary>
    private static async Task<string> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Metrics Test {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise for metrics",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions)
            .GetProperty("id").GetString()!;
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_ExerciseProgress_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/progress");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_InjectMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/injects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ObservationMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/observations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_TimelineMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ControllerMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/controllers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_EvaluatorMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/evaluators");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CapabilityMetrics_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Exercise Progress Tests
    // =========================================================================

    [Fact]
    public async Task GET_ExerciseProgress_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/progress");

        // System Admin bypasses exercise access checks.
        // Service may return null for a new exercise (no progress data), yielding 404.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_ExerciseProgress_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/progress");

        // SysAdmin bypasses AuthorizeExerciseAccess, service returns null -> 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Inject Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_InjectMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/injects");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_InjectMetrics_WithCustomTolerance_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/metrics/injects?onTimeToleranceMinutes=10");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_InjectMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/injects");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Observation Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_ObservationMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/observations");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_ObservationMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/observations");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Timeline Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_TimelineMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/timeline");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_TimelineMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Controller Activity Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_ControllerMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/controllers");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_ControllerMetrics_WithCustomTolerance_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/metrics/controllers?onTimeToleranceMinutes=15");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_ControllerMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/controllers");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Evaluator Coverage Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_EvaluatorMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/evaluators");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_EvaluatorMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/evaluators");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Capability Performance Metrics Tests
    // =========================================================================

    [Fact]
    public async Task GET_CapabilityMetrics_ExistingExercise_ReturnsSuccessOrNotFound()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/capabilities");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CapabilityMetrics_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/metrics/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // End-to-End: Create exercise and query all metric endpoints
    // =========================================================================

    [Fact]
    public async Task EndToEnd_AllMetricEndpoints_ReturnValidResponses()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        // All metrics endpoints should return 200 or 404 (no data for empty exercise)
        var progressResponse = await client.GetAsync($"/api/exercises/{exerciseId}/progress");
        progressResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var injectResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/injects");
        injectResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var observationResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/observations");
        observationResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var timelineResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/timeline");
        timelineResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var controllerResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/controllers");
        controllerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var evaluatorResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/evaluators");
        evaluatorResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        var capabilityResponse = await client.GetAsync($"/api/exercises/{exerciseId}/metrics/capabilities");
        capabilityResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
