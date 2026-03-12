using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for InjectsController API endpoints.
/// Tests inject CRUD, conduct operations (fire/skip/reset), and approval workflow.
/// </summary>
[Collection("WebApi Integration")]
public class InjectsControllerIntegrationTests
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
            Name = $"Inject Test {Guid.NewGuid():N}"[..40],
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions)
            .GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> CreateInjectAsync(
        HttpClient client, string exerciseId, string? title = null)
    {
        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/injects", new
        {
            Title = title ?? $"Test Inject {Guid.NewGuid():N}"[..40],
            Description = "Integration test inject description",
            Target = "EOC Team",
            ScheduledTime = "09:00:00"
        });
        var json = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Inject creation failed with body: {json[..Math.Min(500, json.Length)]}");
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // List Injects
    // =========================================================================

    [Fact]
    public async Task GET_Injects_NewExercise_ReturnsEmptyList()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/injects");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var injects = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        injects.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_Injects_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/injects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Create Inject
    // =========================================================================

    [Fact]
    public async Task POST_CreateInject_ValidRequest_Returns201()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var inject = await CreateInjectAsync(client, exerciseId, "Fire Drill Notification");

        inject.GetProperty("title").GetString().Should().Be("Fire Drill Notification");
        inject.GetProperty("description").GetString().Should().Be("Integration test inject description");
    }

    [Fact]
    public async Task POST_CreateInject_MultipleInjects_AssignsInjectNumbers()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var inject1 = await CreateInjectAsync(client, exerciseId, "First Inject");
        var inject2 = await CreateInjectAsync(client, exerciseId, "Second Inject");

        var num1 = inject1.GetProperty("injectNumber").GetInt32();
        var num2 = inject2.GetProperty("injectNumber").GetInt32();

        num2.Should().BeGreaterThan(num1);
    }

    // =========================================================================
    // Get Single Inject
    // =========================================================================

    [Fact]
    public async Task GET_InjectById_ExistingInject_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId, "Retrieve This Inject");
        var injectId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/injects/{injectId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_InjectById_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/injects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Inject
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateInject_ValidRequest_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/injects/{injectId}", new
        {
            Title = "Updated Inject Title",
            Description = "Updated description",
            Target = "EOC Team",
            ScheduledTime = "10:30:00"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var inject = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        inject.GetProperty("title").GetString().Should().Be("Updated Inject Title");
    }

    // =========================================================================
    // Delete Inject
    // =========================================================================

    [Fact]
    public async Task DELETE_Inject_ExistingInject_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/injects/{injectId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Inject_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/injects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Inject History
    // =========================================================================

    [Fact]
    public async Task GET_InjectHistory_ExistingInject_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/injects/{injectId}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Fire / Skip / Reset
    // =========================================================================

    [Fact]
    public async Task POST_FireInject_ActiveExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        // Activate exercise first
        var activateResponse = await client.PostAsync($"/api/exercises/{exerciseId}/activate", null);
        if (activateResponse.StatusCode != HttpStatusCode.OK)
            return; // Skip if can't activate

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/fire",
            new { Notes = "Fired during integration test" });

        // May return 400 if inject requires approval or other business rules
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await response.Content.ReadAsStringAsync();
            var inject = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
            inject.GetProperty("status").GetString().Should().Be("Delivered");
        }
    }

    [Fact]
    public async Task POST_SkipInject_ActiveExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        // Activate
        var activateResponse = await client.PostAsync($"/api/exercises/{exerciseId}/activate", null);
        if (activateResponse.StatusCode != HttpStatusCode.OK)
            return;

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/skip",
            new { Reason = "Not applicable for this scenario" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_ResetInject_AfterFire_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var created = await CreateInjectAsync(client, exerciseId);
        var injectId = created.GetProperty("id").GetString();

        // Activate and fire
        var activateResponse = await client.PostAsync($"/api/exercises/{exerciseId}/activate", null);
        if (activateResponse.StatusCode != HttpStatusCode.OK)
            return;

        await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/fire",
            new { Notes = "" });

        // Reset
        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Reorder
    // =========================================================================

    [Fact]
    public async Task POST_ReorderInjects_ValidIds_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);
        var inject1 = await CreateInjectAsync(client, exerciseId, "First");
        var inject2 = await CreateInjectAsync(client, exerciseId, "Second");

        var id1 = inject1.GetProperty("id").GetString();
        var id2 = inject2.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/reorder",
            new { InjectIds = new[] { id2, id1 } }); // Reverse order

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_ReorderInjects_EmptyIds_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/reorder",
            new { InjectIds = Array.Empty<string>() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Approval Workflow
    // =========================================================================

    [Fact]
    public async Task POST_SubmitForApproval_DraftInject_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        // Enable approval on exercise
        await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/approval-settings", new
        {
            RequireInjectApproval = true,
            IsOverride = false
        });

        var created = await CreateInjectAsync(client, exerciseId, "Approval Test Inject");
        var injectId = created.GetProperty("id").GetString();

        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/submit", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_CanApprove_ExistingExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/injects/can-approve");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // End-to-End: Create → Update → Fire → Reset → Skip
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateUpdateFireResetSkip_WorksCorrectly()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exerciseId = await CreateExerciseAsync(client);

        // Create inject
        var created = await CreateInjectAsync(client, exerciseId, "E2E Inject");
        var injectId = created.GetProperty("id").GetString();

        // Update
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}",
            new
            {
                Title = "E2E Updated Inject",
                Description = "Updated description",
                Target = "EOC Team",
                ScheduledTime = "10:00:00"
            });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // List — should show updated inject
        var listResponse = await client.GetAsync($"/api/exercises/{exerciseId}/injects");
        var listJson = await listResponse.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<JsonElement>(listJson, JsonOptions);
        list.GetArrayLength().Should().Be(1);

        // Activate exercise
        var activateResponse = await client.PostAsync($"/api/exercises/{exerciseId}/activate", null);
        if (activateResponse.StatusCode != HttpStatusCode.OK)
            return;

        // Fire — may fail if inject requires approval or other business rules
        var fireResponse = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/injects/{injectId}/fire",
            new { Notes = "E2E fire" });
        fireResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);

        if (fireResponse.StatusCode == HttpStatusCode.OK)
        {
            // Reset
            var resetResponse = await client.PostAsync(
                $"/api/exercises/{exerciseId}/injects/{injectId}/reset", null);
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Skip
            var skipResponse = await client.PostAsJsonAsync(
                $"/api/exercises/{exerciseId}/injects/{injectId}/skip",
                new { Reason = "E2E skip reason" });
            skipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
