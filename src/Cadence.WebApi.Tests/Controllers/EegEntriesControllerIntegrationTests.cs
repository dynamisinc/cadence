using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for EegEntriesController API endpoints.
/// Tests CRUD operations for EEG entries (structured evaluations against critical tasks).
/// </summary>
[Collection("WebApi Integration")]
public class EegEntriesControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string AdminEmail, Guid OrgId)>
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

        return (factory, client, email, org.Id);
    }

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client, string? name = null)
    {
        var exerciseName = name ?? $"Test Exercise {Guid.NewGuid():N}"[..40];
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = exerciseName,
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    /// <summary>
    /// Creates an exercise with a capability, capability target, and critical task
    /// to support EEG entry creation.
    /// </summary>
    private static async Task<(string ExerciseId, string CriticalTaskId)>
        CreateExerciseWithCriticalTaskAsync(HttpClient client, Guid orgId)
    {
        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString()!;

        // Create a capability in the org library
        var capResponse = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = $"EEG Capability {Guid.NewGuid():N}"[..40],
            Description = "Capability for EEG testing",
            Category = "Test"
        });
        capResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var capJson = await capResponse.Content.ReadAsStringAsync();
        var capability = JsonSerializer.Deserialize<JsonElement>(capJson, JsonOptions);
        var capabilityId = capability.GetProperty("id").GetString()!;

        // Create a capability target for the exercise
        var targetResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets", new
            {
                CapabilityId = Guid.Parse(capabilityId),
                TargetDescription = "Establish comms within 30 minutes"
            });
        targetResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var targetJson = await targetResponse.Content.ReadAsStringAsync();
        var target = JsonSerializer.Deserialize<JsonElement>(targetJson, JsonOptions);
        var targetId = target.GetProperty("id").GetString()!;

        // Create a critical task under the target
        var taskResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new
            {
                TaskDescription = "Issue EOC activation notification"
            });
        taskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskJson = await taskResponse.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<JsonElement>(taskJson, JsonOptions);
        var taskId = task.GetProperty("id").GetString()!;

        return (exerciseId, taskId);
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_EegEntries_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/eeg-entries");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateEegEntry_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{Guid.NewGuid()}/critical-tasks/{Guid.NewGuid()}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.NewGuid(),
                ObservationText = "Test observation",
                Rating = 0
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_EegCoverage_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/eeg-coverage");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get EEG Entries by Exercise
    // =========================================================================

    [Fact]
    public async Task GET_EegEntries_NewExercise_ReturnsEmptyList()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/eeg-entries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("items").GetArrayLength().Should().Be(0);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // =========================================================================
    // Get EEG Entries by Critical Task
    // =========================================================================

    [Fact]
    public async Task GET_EegEntriesByCriticalTask_NewTask_ReturnsEmptyList()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var response = await client.GetAsync($"/api/critical-tasks/{taskId}/eeg-entries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Create EEG Entry
    // =========================================================================

    [Fact]
    public async Task POST_CreateEegEntry_ValidRequest_Returns201()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "Team activated EOC within 25 minutes",
                Rating = 0 // Performed
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var entry = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        entry.GetProperty("observationText").GetString().Should().Be("Team activated EOC within 25 minutes");
        entry.GetProperty("criticalTaskId").GetString().Should().Be(taskId);
        entry.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_CreateEegEntry_EmptyObservation_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "",
                Rating = 0
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateEegEntry_ObservationTooLong_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = new string('x', 4001),
                Rating = 0
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateEegEntry_MismatchedTaskId_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var mismatchedId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = mismatchedId,
                ObservationText = "Observation with wrong task ID",
                Rating = 0
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get EEG Entry by ID
    // =========================================================================

    [Fact]
    public async Task GET_EegEntryById_ExistingEntry_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "Entry for get by ID test",
                Rating = 1 // SomeChallenges
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var entryId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/eeg-entries/{entryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var entry = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        entry.GetProperty("observationText").GetString().Should().Be("Entry for get by ID test");
    }

    [Fact]
    public async Task GET_EegEntryById_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/eeg-entries/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Get EEG Coverage
    // =========================================================================

    [Fact]
    public async Task GET_EegCoverage_NewExercise_Returns200WithZeroCoverage()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/eeg-coverage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var coverage = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        coverage.GetProperty("totalTasks").GetInt32().Should().Be(0);
        coverage.GetProperty("evaluatedTasks").GetInt32().Should().Be(0);
        coverage.GetProperty("coveragePercentage").GetDecimal().Should().Be(0);
    }

    [Fact]
    public async Task GET_EegCoverage_WithEntry_ReflectsCoverage()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        // Create an EEG entry
        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "Task performed well",
                Rating = 0
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/eeg-coverage");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var coverage = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        coverage.GetProperty("totalTasks").GetInt32().Should().BeGreaterOrEqualTo(1);
        coverage.GetProperty("evaluatedTasks").GetInt32().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // Delete EEG Entry
    // =========================================================================

    [Fact]
    public async Task DELETE_EegEntry_ExistingEntry_Returns204()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "Entry to delete",
                Rating = 0
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var entryId = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/eeg-entries/{entryId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_EegEntry_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/eeg-entries/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_EegEntry_ThenGetById_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, taskId) = await CreateExerciseWithCriticalTaskAsync(client, orgId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries",
            new
            {
                CriticalTaskId = Guid.Parse(taskId),
                ObservationText = "Will be deleted",
                Rating = 2
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var entryId = created.GetProperty("id").GetString();

        await client.DeleteAsync($"/api/exercises/{exerciseId}/eeg-entries/{entryId}");

        var getResponse = await client.GetAsync($"/api/eeg-entries/{entryId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
