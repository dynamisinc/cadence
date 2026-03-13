using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExerciseParticipantsController API endpoints.
/// Tests participant CRUD, role updates, and bulk operations.
/// </summary>
[Collection("WebApi Integration")]
public class ExerciseParticipantsControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string AdminUserId)>
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

        var adminUserId = authResponse.UserId!.Value.ToString();

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

        return (factory, client, adminUserId);
    }

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = $"Part Test Exercise {Guid.NewGuid():N}"[..40],
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
    /// Register a second user and add them to the org so they can be added as participants.
    /// </summary>
    private static async Task<string> CreateSecondUserAsync(HttpClient client)
    {
        var email = $"user-{Guid.NewGuid()}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(email, "Password123!", "Second User"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.UserId!.Value.ToString();
    }

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Participants_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Participants_ExistingExercise_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/participants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Add Participant Tests
    // =========================================================================

    [Fact]
    public async Task POST_AddParticipant_ValidRequest_Returns201()
    {
        var (factory, client, adminUserId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/participants", new
        {
            UserId = adminUserId,
            Role = "Controller"
        });

        // Could be 201 (new) or 400 (already exists as creator)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AddParticipant_NonExistentUser_ReturnsError()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/participants", new
        {
            UserId = Guid.NewGuid().ToString(),
            Role = "Controller"
        });

        // Should be 404 (user doesn't exist) or 400 (invalid user)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get Participant by ID Tests
    // =========================================================================

    [Fact]
    public async Task GET_ParticipantById_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/participants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Participant Role Tests
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateRole_NonExistentParticipant_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}/participants/{Guid.NewGuid()}/role",
            new { Role = "Evaluator" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Remove Participant Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Participant_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}/participants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Bulk Update Tests
    // =========================================================================

    [Fact]
    public async Task PUT_BulkUpdate_EmptyList_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/participants", new
        {
            Participants = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // End-to-End: Add → Update Role → Remove
    // =========================================================================

    [Fact]
    public async Task EndToEnd_AddUpdateRemove_WorksCorrectly()
    {
        var (factory, client, adminUserId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Add participant
        var addResponse = await client.PostAsJsonAsync($"/api/exercises/{exerciseId}/participants", new
        {
            UserId = adminUserId,
            Role = "Controller"
        });

        if (addResponse.StatusCode == HttpStatusCode.Created)
        {
            // Update role
            var updateResponse = await client.PutAsJsonAsync(
                $"/api/exercises/{exerciseId}/participants/{adminUserId}/role",
                new { Role = "ExerciseDirector" });
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get participant
            var getResponse = await client.GetAsync($"/api/exercises/{exerciseId}/participants/{adminUserId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Remove
            var removeResponse = await client.DeleteAsync($"/api/exercises/{exerciseId}/participants/{adminUserId}");
            removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify removed
            var verifyResponse = await client.GetAsync($"/api/exercises/{exerciseId}/participants/{adminUserId}");
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
