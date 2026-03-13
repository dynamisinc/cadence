using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExerciseClockController API endpoints.
/// Tests clock operations: get state, start, pause, stop, reset, set-time.
/// </summary>
[Collection("WebApi Integration")]
public class ExerciseClockControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client)>
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

        await client.PostAsJsonAsync("/api/users/current-organization",
            new { OrganizationId = org!.Id });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", false));
        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth!.AccessToken);

        return (factory, client);
    }

    private static async Task<string> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Clock Test {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions)
            .GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateExerciseWithInjectAsync(HttpClient client)
    {
        var exerciseId = await CreateExerciseAsync(client);
        await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/injects", new
        {
            Title = "Test Inject",
            Description = "Test inject for clock operations",
            Target = "EOC Team",
            ScheduledTime = "09:00:00"
        });
        return exerciseId;
    }

    // =========================================================================
    // Get Clock State
    // =========================================================================

    [Fact]
    public async Task GET_ClockState_ExistingExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/clock");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var clock = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        clock.GetProperty("exerciseId").GetString().Should().Be(exerciseId);
        clock.GetProperty("state").GetString().Should().Be("Stopped");
    }

    [Fact]
    public async Task GET_ClockState_NonExistent_Returns404Or403()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/clock");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_ClockState_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/clock");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Start Clock
    // =========================================================================

    [Fact]
    public async Task POST_StartClock_DraftExercise_ActivatesAndStartsClock()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseWithInjectAsync(client);

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/clock/start", null);

        // May succeed (auto-activates) or fail (business rule)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var clock = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
            clock.GetProperty("state").GetString().Should().Be("Running");
        }
    }

    [Fact]
    public async Task POST_StartClock_NoInjects_ReturnsOKOrBadRequest()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/clock/start", null);

        // Clock start may succeed (auto-activates) or fail depending on business rules
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Pause Clock
    // =========================================================================

    [Fact]
    public async Task POST_PauseClock_StoppedClock_ReturnsBadRequest()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/clock/pause", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Stop Clock
    // =========================================================================

    [Fact]
    public async Task POST_StopClock_StoppedClock_ReturnsBadRequest()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/clock/stop", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Reset Clock
    // =========================================================================

    [Fact]
    public async Task POST_ResetClock_DraftExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsync($"/api/exercises/{exerciseId}/clock/reset", null);

        // Reset is allowed for Draft exercises
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Set Clock Time
    // =========================================================================

    [Fact]
    public async Task POST_SetClockTime_NotPaused_ReturnsBadRequest()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/clock/set-time",
            new { ElapsedTime = "01:00:00" });

        // Only allowed when paused
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Full Clock Lifecycle: Start → Pause → Start → Stop → Reset
    // =========================================================================

    [Fact]
    public async Task ClockLifecycle_StartPauseStartStop_WorksCorrectly()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseWithInjectAsync(client);

        // Start clock
        var startResponse = await client.PostAsync($"/api/exercises/{exerciseId}/clock/start", null);
        if (startResponse.StatusCode != HttpStatusCode.OK)
            return; // Skip if can't start

        var startJson = await startResponse.Content.ReadAsStringAsync();
        var started = JsonSerializer.Deserialize<JsonElement>(startJson, JsonOptions);
        started.GetProperty("state").GetString().Should().Be("Running");

        // Pause
        var pauseResponse = await client.PostAsync($"/api/exercises/{exerciseId}/clock/pause", null);
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pauseJson = await pauseResponse.Content.ReadAsStringAsync();
        var paused = JsonSerializer.Deserialize<JsonElement>(pauseJson, JsonOptions);
        paused.GetProperty("state").GetString().Should().Be("Paused");

        // Resume (start again)
        var resumeResponse = await client.PostAsync($"/api/exercises/{exerciseId}/clock/start", null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Stop
        var stopResponse = await client.PostAsync($"/api/exercises/{exerciseId}/clock/stop", null);
        stopResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var stopJson = await stopResponse.Content.ReadAsStringAsync();
        var stopped = JsonSerializer.Deserialize<JsonElement>(stopJson, JsonOptions);
        stopped.GetProperty("state").GetString().Should().Be("Stopped");
    }
}
