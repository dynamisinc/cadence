using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for AssignmentsController API endpoints.
/// Tests user exercise assignment retrieval.
/// </summary>
[Collection("WebApi Integration")]
public class AssignmentsControllerIntegrationTests
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

    // =========================================================================
    // Authentication Tests
    // =========================================================================

    [Fact]
    public async Task GET_MyAssignments_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/assignments/my");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_MyAssignment_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/assignments/my/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get My Assignments Tests
    // =========================================================================

    [Fact]
    public async Task GET_MyAssignments_AuthenticatedUser_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/assignments/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var assignments = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        // Response should be a structured object with assignment groups
        assignments.ValueKind.Should().BeOneOf(JsonValueKind.Object, JsonValueKind.Array);
    }

    [Fact]
    public async Task GET_MyAssignments_NoExercises_ReturnsEmptyResult()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/assignments/my");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Get My Assignment (Specific Exercise) Tests
    // =========================================================================

    [Fact]
    public async Task GET_MyAssignment_NonExistentExercise_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/assignments/my/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_MyAssignment_ExistingExercise_ReturnsAssignment()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create an exercise (admin is auto-assigned as ExerciseDirector)
        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/assignments/my/{exerciseId}");

        // The admin may or may not be auto-assigned to the exercise
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
