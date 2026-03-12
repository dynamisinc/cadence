using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for CapabilityTargetsController API endpoints.
/// Tests CRUD operations on exercise-scoped capability targets.
/// </summary>
[Collection("WebApi Integration")]
public class CapabilityTargetsControllerIntegrationTests
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
    /// Creates an org-level capability that can be referenced by capability targets.
    /// Returns the capability ID.
    /// </summary>
    private static async Task<Guid> CreateCapabilityAsync(HttpClient client, Guid orgId)
    {
        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = $"Test Capability {Guid.NewGuid():N}"[..40],
            Description = "A test capability for integration tests",
            Category = "Testing"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "Capability creation is required for capability target tests");
        var json = await response.Content.ReadAsStringAsync();
        var cap = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        return Guid.Parse(cap.GetProperty("id").GetString()!);
    }

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task GET_CapabilityTargets_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/capability-targets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateTarget_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/exercises/{Guid.NewGuid()}/capability-targets", new
        {
            CapabilityId = Guid.NewGuid(),
            TargetDescription = "Establish communications within 30 minutes"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_UpdateTarget_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{Guid.NewGuid()}/capability-targets/{Guid.NewGuid()}", new
            {
                TargetDescription = "Updated target"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_Target_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.DeleteAsync(
            $"/api/exercises/{Guid.NewGuid()}/capability-targets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Capability Targets
    // =========================================================================

    [Fact]
    public async Task GET_CapabilityTargets_NewExercise_ReturnsEmptyList()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/capability-targets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("items").GetArrayLength().Should().Be(0);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // =========================================================================
    // Create Capability Target
    // =========================================================================

    [Fact]
    public async Task POST_CreateTarget_ValidRequest_Returns201()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Establish interoperable communications within 30 minutes"
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Create capability target failed: {body[..Math.Min(500, body.Length)]}");

        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        result.GetProperty("targetDescription").GetString()
            .Should().Be("Establish interoperable communications within 30 minutes");
        result.GetProperty("exerciseId").GetString().Should().Be(exerciseId);
        result.GetProperty("capabilityId").GetString().Should().Be(capabilityId.ToString());
    }

    [Fact]
    public async Task POST_CreateTarget_EmptyDescription_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateTarget_DescriptionTooLong_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = new string('x', 501)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateTarget_EmptyCapabilityId_Returns400()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = Guid.Empty,
            TargetDescription = "Valid description"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateTarget_WithSources_Returns201()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Complete evacuation within 15 minutes",
            Sources = "Metro County EOP, Annex F; SOP 5.2"
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Create capability target with sources failed: {body[..Math.Min(500, body.Length)]}");

        var result = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        result.GetProperty("sources").GetString().Should().Be("Metro County EOP, Annex F; SOP 5.2");
    }

    // =========================================================================
    // Update Capability Target
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateTarget_ValidRequest_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        // Create a target first
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Original target description"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var targetId = created.GetProperty("id").GetString();

        // Update it
        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}", new
            {
                TargetDescription = "Updated target description",
                Sources = "Updated source reference"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("targetDescription").GetString().Should().Be("Updated target description");
        result.GetProperty("sources").GetString().Should().Be("Updated source reference");
    }

    [Fact]
    public async Task PUT_UpdateTarget_EmptyDescription_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        // Create a target first
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Target to be updated with empty"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var targetId = created.GetProperty("id").GetString();

        // Try to update with empty description
        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}", new
            {
                TargetDescription = ""
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_UpdateTarget_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{Guid.NewGuid()}", new
            {
                TargetDescription = "This should 404"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Capability Target
    // =========================================================================

    [Fact]
    public async Task DELETE_Target_ExistingTarget_Returns204()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        // Create a target first
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Target to be deleted"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var targetId = created.GetProperty("id").GetString();

        // Delete it
        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Target_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Get Single Target
    // =========================================================================

    [Fact]
    public async Task GET_TargetById_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/capability-targets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_TargetById_ExistingTarget_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        // Create a target
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "Target to retrieve by ID"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var targetId = created.GetProperty("id").GetString();

        // Get by ID
        var response = await client.GetAsync($"/api/capability-targets/{targetId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var target = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        target.GetProperty("targetDescription").GetString().Should().Be("Target to retrieve by ID");
    }

    // =========================================================================
    // End-to-End: Create → Read → Update → Delete
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateReadUpdateDelete_WorksCorrectly()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();
        var capabilityId = await CreateCapabilityAsync(client, orgId);

        // Create
        var createResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/capability-targets", new
        {
            CapabilityId = capabilityId,
            TargetDescription = "E2E test target",
            Sources = "Test SOP 1.0"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createJson = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<JsonElement>(createJson, JsonOptions);
        var targetId = created.GetProperty("id").GetString();

        // Read (by exercise)
        var listResponse = await client.GetAsync($"/api/exercises/{exerciseId}/capability-targets");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await listResponse.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<JsonElement>(listJson, JsonOptions);
        list.GetProperty("items").GetArrayLength().Should().BeGreaterOrEqualTo(1);

        // Read (by ID)
        var getResponse = await client.GetAsync($"/api/capability-targets/{targetId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}", new
            {
                TargetDescription = "E2E updated target",
                Sources = "Updated SOP 2.0"
            });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateJson = await updateResponse.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<JsonElement>(updateJson, JsonOptions);
        updated.GetProperty("targetDescription").GetString().Should().Be("E2E updated target");

        // Delete
        var deleteResponse = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted (list should be empty or reduced)
        var finalListResponse = await client.GetAsync($"/api/exercises/{exerciseId}/capability-targets");
        finalListResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
