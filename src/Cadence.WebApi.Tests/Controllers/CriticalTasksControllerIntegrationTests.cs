using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for CriticalTasksController API endpoints.
/// Tests CRUD operations for critical tasks under capability targets.
/// </summary>
[Collection("WebApi Integration")]
public class CriticalTasksControllerIntegrationTests
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

    private static async Task<(string ExerciseId, string TargetId)> CreateExerciseWithCapabilityTargetAsync(
        HttpClient client, Guid orgId)
    {
        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString()!;

        // Create a capability in the org library
        var capResponse = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = $"Test Capability {Guid.NewGuid():N}"[..40],
            Description = "Test capability for critical tasks",
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

        return (exerciseId, targetId);
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_CriticalTasksByTarget_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/capability-targets/{Guid.NewGuid()}/critical-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CriticalTasksByExercise_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/critical-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateCriticalTask_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{Guid.NewGuid()}/capability-targets/{Guid.NewGuid()}/critical-tasks",
            new { TaskDescription = "Test task" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Critical Tasks by Capability Target
    // =========================================================================

    [Fact]
    public async Task GET_CriticalTasksByTarget_NewTarget_ReturnsEmptyList()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var response = await client.GetAsync($"/api/capability-targets/{targetId}/critical-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("items").GetArrayLength().Should().Be(0);
        result.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    // =========================================================================
    // Get Critical Tasks by Exercise
    // =========================================================================

    [Fact]
    public async Task GET_CriticalTasksByExercise_NewExercise_ReturnsEmptyList()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/critical-tasks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Create Critical Task
    // =========================================================================

    [Fact]
    public async Task POST_CreateCriticalTask_ValidRequest_Returns201()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new
            {
                TaskDescription = "Issue EOC activation notification",
                Standard = "Per SOP 5.2"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        task.GetProperty("taskDescription").GetString().Should().Be("Issue EOC activation notification");
        task.GetProperty("standard").GetString().Should().Be("Per SOP 5.2");
        task.GetProperty("capabilityTargetId").GetString().Should().Be(targetId);
    }

    [Fact]
    public async Task POST_CreateCriticalTask_EmptyDescription_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateCriticalTask_DescriptionTooLong_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = new string('x', 501) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateCriticalTask_StandardTooLong_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new
            {
                TaskDescription = "Valid description",
                Standard = new string('x', 1001)
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get Critical Task by ID
    // =========================================================================

    [Fact]
    public async Task GET_CriticalTaskById_ExistingTask_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        // Create a task
        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Test task for get by ID" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/critical-tasks/{taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        task.GetProperty("taskDescription").GetString().Should().Be("Test task for get by ID");
    }

    [Fact]
    public async Task GET_CriticalTaskById_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/critical-tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Critical Task
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateCriticalTask_ValidRequest_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        // Create a task
        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Original task description" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        // Update it
        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}",
            new
            {
                TaskDescription = "Updated task description",
                Standard = "Updated standard"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        task.GetProperty("taskDescription").GetString().Should().Be("Updated task description");
        task.GetProperty("standard").GetString().Should().Be("Updated standard");
    }

    [Fact]
    public async Task PUT_UpdateCriticalTask_EmptyDescription_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Original description" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}",
            new { TaskDescription = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_UpdateCriticalTask_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{Guid.NewGuid()}",
            new { TaskDescription = "Does not matter" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Critical Task
    // =========================================================================

    [Fact]
    public async Task DELETE_CriticalTask_ExistingTask_Returns204()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        // Create a task
        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Task to be deleted" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        // Delete it
        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_CriticalTask_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync(
            $"/api/exercises/{exerciseId}/critical-tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_CriticalTask_ThenGetById_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        // Create and delete
        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Will be deleted" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        await client.DeleteAsync($"/api/exercises/{exerciseId}/critical-tasks/{taskId}");

        // Verify it's gone
        var getResponse = await client.GetAsync($"/api/critical-tasks/{taskId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Linked Injects
    // =========================================================================

    [Fact]
    public async Task GET_LinkedInjects_NewTask_ReturnsEmptyList()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var (exerciseId, targetId) = await CreateExerciseWithCapabilityTargetAsync(client, orgId);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks",
            new { TaskDescription = "Task for linked injects test" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var taskId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/critical-tasks/{taskId}/injects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var injectIds = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        injectIds.GetArrayLength().Should().Be(0);
    }
}
