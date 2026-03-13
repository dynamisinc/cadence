using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ObservationsController API endpoints.
/// Tests observation CRUD and org-scoped access control.
/// </summary>
[Collection("WebApi Integration")]
public class ObservationsControllerIntegrationTests
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
            Name = $"Obs Test {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions)
            .GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Creates an exercise with an inject and activates it.
    /// Observations require an Active or Paused exercise.
    /// </summary>
    private static async Task<string> CreateActiveExerciseAsync(HttpClient client)
    {
        var exerciseId = await CreateExerciseAsync(client);

        // Create inject (required for activation)
        var injectResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/injects", new
        {
            Title = "Test Inject for Observations",
            Description = "Inject to enable exercise activation",
            Target = "EOC Team",
            ScheduledTime = "09:00:00"
        });
        injectResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Activate the exercise
        var activateResponse = await client.PostAsync($"/api/exercises/{exerciseId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Exercise must be activated before observations can be added");

        return exerciseId;
    }

    // =========================================================================
    // Get Observations
    // =========================================================================

    [Fact]
    public async Task GET_ObservationsByExercise_NewExercise_ReturnsEmptyList()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/observations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var observations = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        observations.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_ObservationsByExercise_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/observations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Create Observation
    // =========================================================================

    [Fact]
    public async Task POST_CreateObservation_ValidRequest_Returns201()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "Test observation content for integration test",
            Rating = 0, // Performed
            Location = "Main EOC"
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Observation creation failed with body: {body[..Math.Min(500, body.Length)]}");

        var observation = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        observation.GetProperty("content").GetString().Should().Be("Test observation content for integration test");
        observation.GetProperty("exerciseId").GetString().Should().Be(exerciseId);
    }

    [Fact]
    public async Task POST_CreateObservation_EmptyContent_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateObservation_ContentTooLong_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = new string('x', 4001),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get Single Observation
    // =========================================================================

    [Fact]
    public async Task GET_ObservationById_ExistingObservation_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        // Create observation
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "Observation to retrieve by ID"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var observationId = created.GetProperty("id").GetString();

        // Get by ID
        var response = await client.GetAsync($"/api/observations/{observationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var observation = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        observation.GetProperty("content").GetString().Should().Be("Observation to retrieve by ID");
    }

    [Fact]
    public async Task GET_ObservationById_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/observations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Observation
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateObservation_ValidRequest_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        // Create
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "Original observation content"
        });
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var observationId = created.GetProperty("id").GetString();

        // Update
        var response = await client.PutAsJsonAsync($"/api/observations/{observationId}", new
        {
            Content = "Updated observation content",
            Rating = 1, // Satisfactory
            Location = "Updated Location"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var observation = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        observation.GetProperty("content").GetString().Should().Be("Updated observation content");
    }

    [Fact]
    public async Task PUT_UpdateObservation_EmptyContent_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "Will try to clear this"
        });
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var observationId = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/observations/{observationId}", new
        {
            Content = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_UpdateObservation_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync($"/api/observations/{Guid.NewGuid()}", new
        {
            Content = "This should 404"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Observation
    // =========================================================================

    [Fact]
    public async Task DELETE_Observation_ExistingObservation_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "Observation to be deleted"
        });
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var observationId = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/observations/{observationId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone (soft delete — should return 404)
        var getResponse = await client.GetAsync($"/api/observations/{observationId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Observation_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/observations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // End-to-End: Create → Read → Update → Delete
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateReadUpdateDelete_WorksCorrectly()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateActiveExerciseAsync(client);

        // Create
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/observations", new
        {
            Content = "E2E test observation",
            Location = "Test Room A"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var observationId = created.GetProperty("id").GetString();

        // Read (by exercise)
        var listResponse = await client.GetAsync($"/api/exercises/{exerciseId}/observations");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResponse.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<JsonElement>(listJson, JsonOptions);
        list.GetArrayLength().Should().BeGreaterOrEqualTo(1);

        // Read (by ID)
        var getResponse = await client.GetAsync($"/api/observations/{observationId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/observations/{observationId}", new
        {
            Content = "E2E updated observation",
            Location = "Test Room B"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<JsonElement>(updateJson, JsonOptions);
        updated.GetProperty("content").GetString().Should().Be("E2E updated observation");

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/observations/{observationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
