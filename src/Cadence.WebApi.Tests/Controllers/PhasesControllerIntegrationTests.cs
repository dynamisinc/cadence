using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for PhasesController API endpoints.
/// Tests CRUD, reorder, and validation for exercise phases.
/// </summary>
[Collection("WebApi Integration")]
public class PhasesControllerIntegrationTests
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
            Name = $"Phase Test Exercise {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    private static async Task<JsonElement> CreatePhaseAsync(HttpClient client, string exerciseId, string? name = null)
    {
        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/phases", new
        {
            Name = name ?? $"Test Phase {Guid.NewGuid():N}"[..30],
            Description = "Test phase description"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Phases_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/phases");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Phases_NonExistentExercise_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/phases");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Phases_ExistingExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/phases");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Create Phase Tests
    // =========================================================================

    [Fact]
    public async Task POST_CreatePhase_ValidRequest_Returns201()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/phases", new
        {
            Name = "Phase Alpha",
            Description = "First phase of the exercise"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var phase = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        phase.GetProperty("name").GetString().Should().Be("Phase Alpha");
        phase.GetProperty("description").GetString().Should().Be("First phase of the exercise");
        phase.GetProperty("sequence").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task POST_CreatePhase_NonExistentExercise_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/exercises/{Guid.NewGuid()}/phases", new
        {
            Name = "Ghost Phase",
            Description = "Should not be created"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Get Phase by ID Tests
    // =========================================================================

    [Fact]
    public async Task GET_PhaseById_ExistingPhase_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var phase = await CreatePhaseAsync(client, exerciseId!, "Specific Phase");

        var phaseId = phase.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/phases/{phaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("name").GetString().Should().Be("Specific Phase");
    }

    [Fact]
    public async Task GET_PhaseById_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/phases/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Phase Tests
    // =========================================================================

    [Fact]
    public async Task PUT_UpdatePhase_ValidRequest_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var phase = await CreatePhaseAsync(client, exerciseId!);
        var phaseId = phase.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/phases/{phaseId}", new
        {
            Name = "Updated Phase Name",
            Description = "Updated description"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("name").GetString().Should().Be("Updated Phase Name");
    }

    [Fact]
    public async Task PUT_UpdatePhase_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/phases/{Guid.NewGuid()}", new
        {
            Name = "Ghost Phase Update"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Phase Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Phase_ExistingPhase_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var phase = await CreatePhaseAsync(client, exerciseId!);
        var phaseId = phase.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/phases/{phaseId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/exercises/{exerciseId}/phases/{phaseId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Phase_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/phases/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Reorder Phases Tests
    // =========================================================================

    [Fact]
    public async Task PUT_ReorderPhases_ValidRequest_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var phase1 = await CreatePhaseAsync(client, exerciseId!, "Phase A");
        var phase2 = await CreatePhaseAsync(client, exerciseId!, "Phase B");
        var id1 = phase1.GetProperty("id").GetString();
        var id2 = phase2.GetProperty("id").GetString();

        // Reorder: put phase B first
        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/phases/reorder", new
        {
            PhaseIds = new[] { Guid.Parse(id2!), Guid.Parse(id1!) }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var phases = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        phases.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task PUT_ReorderPhases_NonExistentExercise_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync($"/api/exercises/{Guid.NewGuid()}/phases/reorder", new
        {
            PhaseIds = new[] { Guid.NewGuid() }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // End-to-End: Create → Update → Reorder → Delete
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateUpdateReorderDelete_WorksCorrectly()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Create two phases
        var phase1 = await CreatePhaseAsync(client, exerciseId!, "E2E Phase 1");
        var phase2 = await CreatePhaseAsync(client, exerciseId!, "E2E Phase 2");
        var id1 = phase1.GetProperty("id").GetString();
        var id2 = phase2.GetProperty("id").GetString();

        // Update phase 1
        var updateResponse = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/phases/{id1}", new
        {
            Name = "E2E Phase 1 Updated"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // List phases
        var listResponse = await client.GetAsync($"/api/exercises/{exerciseId}/phases");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResponse.Content.ReadAsStringAsync();
        var phases = JsonSerializer.Deserialize<JsonElement>(listJson, JsonOptions);
        phases.GetArrayLength().Should().Be(2);

        // Reorder
        var reorderResponse = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/phases/reorder", new
        {
            PhaseIds = new[] { Guid.Parse(id2!), Guid.Parse(id1!) }
        });
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete phase 2
        var deleteResponse = await client.DeleteAsync($"/api/exercises/{exerciseId}/phases/{id2}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify only phase 1 remains
        var finalList = await client.GetAsync($"/api/exercises/{exerciseId}/phases");
        var finalJson = await finalList.Content.ReadAsStringAsync();
        var remaining = JsonSerializer.Deserialize<JsonElement>(finalJson, JsonOptions);
        remaining.GetArrayLength().Should().Be(1);
    }
}
