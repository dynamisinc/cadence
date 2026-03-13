using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExerciseCapabilitiesController API endpoints.
/// Tests getting/setting target capabilities and capability summary.
/// </summary>
[Collection("WebApi Integration")]
public class ExerciseCapabilitiesControllerIntegrationTests
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
            Name = $"Cap Test Exercise {Guid.NewGuid():N}"[..40],
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
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Capabilities_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/exercises/{Guid.NewGuid()}/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Capabilities_ExistingExercise_Returns200()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Get Target Capabilities Tests
    // =========================================================================

    [Fact]
    public async Task GET_TargetCapabilities_NewExercise_ReturnsEmptyList()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var capabilities = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        capabilities.GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Set Target Capabilities Tests
    // =========================================================================

    [Fact]
    public async Task PUT_SetCapabilities_EmptyList_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/capabilities", new
        {
            CapabilityIds = Array.Empty<Guid>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PUT_SetCapabilities_WithIds_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // First, get the list of available capabilities (org-level)
        var capsResponse = await client.GetAsync("/api/capabilities");
        if (capsResponse.StatusCode == HttpStatusCode.OK)
        {
            var capsJson = await capsResponse.Content.ReadAsStringAsync();
            var caps = JsonSerializer.Deserialize<JsonElement>(capsJson, JsonOptions);

            if (caps.ValueKind == JsonValueKind.Array && caps.GetArrayLength() > 0)
            {
                var capId = caps[0].GetProperty("id").GetString();
                var response = await client.PutAsJsonAsync($"/api/exercises/{exerciseId}/capabilities", new
                {
                    CapabilityIds = new[] { Guid.Parse(capId!) }
                });

                response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }
        }
    }

    // =========================================================================
    // Capability Summary Tests
    // =========================================================================

    [Fact]
    public async Task GET_CapabilitySummary_NewExercise_Returns200WithZeroCounts()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/exercises/{exerciseId}/capabilities/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var summary = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        summary.GetProperty("targetCount").GetInt32().Should().Be(0);
        summary.GetProperty("evaluatedCount").GetInt32().Should().Be(0);
    }
}
