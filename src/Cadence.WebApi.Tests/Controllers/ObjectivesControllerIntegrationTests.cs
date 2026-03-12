using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ObjectivesController API endpoints.
/// Tests CRUD, number uniqueness check, and summaries for exercise objectives.
/// </summary>
[Collection("WebApi Integration")]
public class ObjectivesControllerIntegrationTests
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

        var switchResponse = await client.PostAsJsonAsync("/api/users/current-organization",
            new { OrganizationId = org!.Id });
        switchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, "Password123!", false));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuth!.AccessToken);

        return (factory, client);
    }

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Obj Test Exercise {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    private static async Task<JsonElement> CreateObjectiveAsync(HttpClient client, string exerciseId, string? name = null, string? number = null)
    {
        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/objectives", new
        {
            Name = name ?? $"Test Objective {Guid.NewGuid():N}"[..40],
            ObjectiveNumber = number,
            Description = "Test objective description"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Objectives_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/objectives");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Objectives_ExistingExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Create Objective Tests
    // =========================================================================

    [Fact]
    public async Task POST_CreateObjective_ValidRequest_Returns201()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/objectives", new
        {
            Name = "Test Response Capability",
            ObjectiveNumber = "OBJ-1",
            Description = "Evaluate response capability"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var objective = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        objective.GetProperty("name").GetString().Should().Be("Test Response Capability");
        objective.GetProperty("objectiveNumber").GetString().Should().Be("OBJ-1");
    }

    [Fact]
    public async Task POST_CreateObjective_EmptyName_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/objectives", new
        {
            Name = "",
            ObjectiveNumber = "1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateObjective_NameTooShort_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/objectives", new
        {
            Name = "AB"  // Less than 3 chars
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get Objective by ID Tests
    // =========================================================================

    [Fact]
    public async Task GET_ObjectiveById_ExistingObjective_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var objective = await CreateObjectiveAsync(client, exerciseId!, "Specific Objective", "OBJ-A");

        var objectiveId = objective.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/{objectiveId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("name").GetString().Should().Be("Specific Objective");
    }

    [Fact]
    public async Task GET_ObjectiveById_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Summaries Tests
    // =========================================================================

    [Fact]
    public async Task GET_ObjectiveSummaries_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        await CreateObjectiveAsync(client, exerciseId!, "Summary Obj 1");
        await CreateObjectiveAsync(client, exerciseId!, "Summary Obj 2");

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/summaries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var summaries = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        summaries.GetArrayLength().Should().BeGreaterOrEqualTo(2);
    }

    // =========================================================================
    // Update Objective Tests
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateObjective_ValidRequest_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var objective = await CreateObjectiveAsync(client, exerciseId!);
        var objectiveId = objective.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/objectives/{objectiveId}", new
        {
            Name = "Updated Objective Name",
            ObjectiveNumber = "UPD-1",
            Description = "Updated description"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("name").GetString().Should().Be("Updated Objective Name");
    }

    [Fact]
    public async Task PUT_UpdateObjective_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/objectives/{Guid.NewGuid()}", new
        {
            Name = "Ghost Objective Update",
            ObjectiveNumber = "X"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Objective Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Objective_ExistingObjective_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var objective = await CreateObjectiveAsync(client, exerciseId!);
        var objectiveId = objective.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/objectives/{objectiveId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/{objectiveId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Objective_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/objectives/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Check Number Uniqueness Tests
    // =========================================================================

    [Fact]
    public async Task GET_CheckNumber_Available_ReturnsTrue()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/check-number?number=UNIQUE-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GET_CheckNumber_EmptyNumber_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/check-number?number=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // End-to-End: Create → Update → Delete
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateUpdateDelete_WorksCorrectly()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Create
        var obj = await CreateObjectiveAsync(client, exerciseId!, "E2E Objective", "E2E-1");
        var objId = obj.GetProperty("id").GetString();

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/objectives/{objId}", new
        {
            Name = "E2E Objective Updated",
            ObjectiveNumber = "E2E-1A",
            Description = "Updated for E2E test"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update
        var getResponse = await client.GetAsync($"/api/exercises/{exerciseId}/objectives/{objId}");
        var getJson = await getResponse.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<JsonElement>(getJson, JsonOptions);
        updated.GetProperty("name").GetString().Should().Be("E2E Objective Updated");

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/exercises/{exerciseId}/objectives/{objId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        (await client.GetAsync($"/api/exercises/{exerciseId}/objectives/{objId}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }
}
