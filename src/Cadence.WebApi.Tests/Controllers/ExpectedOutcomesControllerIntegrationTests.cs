using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExpectedOutcomesController API endpoints.
/// Tests CRUD operations for expected outcomes on injects.
/// </summary>
[Collection("WebApi Integration")]
public class ExpectedOutcomesControllerIntegrationTests
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

    private static async Task<(string ExerciseId, string InjectId)> CreateExerciseWithInjectAsync(HttpClient client)
    {
        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString()!;

        var injectResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/injects", new
        {
            Title = $"Test Inject {Guid.NewGuid():N}"[..40],
            Description = "Inject for expected outcomes testing",
            Target = "EOC Team",
            ScheduledTime = "09:00:00"
        });
        var injectJson = await injectResponse.Content.ReadAsStringAsync();
        injectResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Inject creation failed with body: {injectJson[..Math.Min(500, injectJson.Length)]}");
        var inject = JsonSerializer.Deserialize<JsonElement>(injectJson, JsonOptions);
        var injectId = inject.GetProperty("id").GetString()!;

        return (exerciseId, injectId);
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Outcomes_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/injects/{Guid.NewGuid()}/outcomes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateOutcome_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/injects/{Guid.NewGuid()}/outcomes",
            new { Description = "Test outcome" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Outcomes
    // =========================================================================

    [Fact]
    public async Task GET_Outcomes_NonExistentInject_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/injects/{Guid.NewGuid()}/outcomes");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Outcomes_ExistingInject_ReturnsEmptyList()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.GetAsync($"/api/injects/{injectId}/outcomes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var outcomes = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcomes.GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Create Outcome
    // =========================================================================

    [Fact]
    public async Task POST_CreateOutcome_ValidRequest_Returns201()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes", new
        {
            Description = "Players correctly activate EOC"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var outcome = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcome.GetProperty("description").GetString().Should().Be("Players correctly activate EOC");
        outcome.GetProperty("injectId").GetString().Should().Be(injectId);
        outcome.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_CreateOutcome_NonExistentInject_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/injects/{Guid.NewGuid()}/outcomes", new
        {
            Description = "Some outcome"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateOutcome_MultipleOutcomes_AllReturnedOnGet()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome 1" });
        await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome 2" });
        await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome 3" });

        var response = await client.GetAsync($"/api/injects/{injectId}/outcomes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var outcomes = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcomes.GetArrayLength().Should().Be(3);
    }

    // =========================================================================
    // Get Single Outcome
    // =========================================================================

    [Fact]
    public async Task GET_SingleOutcome_ExistingOutcome_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome for single get" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var outcomeId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/injects/{injectId}/outcomes/{outcomeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var outcome = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcome.GetProperty("description").GetString().Should().Be("Outcome for single get");
    }

    [Fact]
    public async Task GET_SingleOutcome_NonExistentOutcome_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.GetAsync($"/api/injects/{injectId}/outcomes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Outcome
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateOutcome_ValidRequest_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Original description" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var outcomeId = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"/api/injects/{injectId}/outcomes/{outcomeId}",
            new { Description = "Updated description" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var outcome = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcome.GetProperty("description").GetString().Should().Be("Updated description");
    }

    [Fact]
    public async Task PUT_UpdateOutcome_NonExistentOutcome_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/injects/{injectId}/outcomes/{Guid.NewGuid()}",
            new { Description = "Does not matter" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateOutcome_NonExistentInject_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync(
            $"/api/injects/{Guid.NewGuid()}/outcomes/{Guid.NewGuid()}",
            new { Description = "Does not matter" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Evaluate Outcome
    // =========================================================================

    [Fact]
    public async Task POST_EvaluateOutcome_SetAchieved_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome to evaluate" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var outcomeId = created.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync(
            $"/api/injects/{injectId}/outcomes/{outcomeId}/evaluate",
            new
            {
                WasAchieved = true,
                EvaluatorNotes = "Well executed"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var outcome = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        outcome.GetProperty("wasAchieved").GetBoolean().Should().BeTrue();
        outcome.GetProperty("evaluatorNotes").GetString().Should().Be("Well executed");
    }

    [Fact]
    public async Task POST_EvaluateOutcome_NonExistentOutcome_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/injects/{injectId}/outcomes/{Guid.NewGuid()}/evaluate",
            new { WasAchieved = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Outcome
    // =========================================================================

    [Fact]
    public async Task DELETE_Outcome_ExistingOutcome_Returns204()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Outcome to delete" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var outcomeId = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/injects/{injectId}/outcomes/{outcomeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Outcome_NonExistentOutcome_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var response = await client.DeleteAsync($"/api/injects/{injectId}/outcomes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Outcome_ThenGetById_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (_, injectId) = await CreateExerciseWithInjectAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/injects/{injectId}/outcomes",
            new { Description = "Will be deleted" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var outcomeId = created.GetProperty("id").GetString();

        await client.DeleteAsync($"/api/injects/{injectId}/outcomes/{outcomeId}");

        var getResponse = await client.GetAsync($"/api/injects/{injectId}/outcomes/{outcomeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Outcome_NonExistentInject_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync(
            $"/api/injects/{Guid.NewGuid()}/outcomes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
