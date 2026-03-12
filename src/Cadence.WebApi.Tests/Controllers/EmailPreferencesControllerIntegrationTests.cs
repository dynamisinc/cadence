using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for EmailPreferencesController API endpoints.
/// Tests user email preference retrieval and updates.
/// </summary>
[Collection("WebApi Integration")]
public class EmailPreferencesControllerIntegrationTests
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

    [Fact]
    public async Task GetPreferences_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me/email-preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferences_Authenticated_Returns200WithAllCategories()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users/me/email-preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        result.TryGetProperty("preferences", out var prefs).Should().BeTrue();
        prefs.ValueKind.Should().Be(JsonValueKind.Array);
        prefs.GetArrayLength().Should().Be(7, "there are 7 email categories");

        // Each preference should have expected properties
        var first = prefs[0];
        first.TryGetProperty("category", out var p1).Should().BeTrue();
        first.TryGetProperty("displayName", out var p2).Should().BeTrue();
        first.TryGetProperty("description", out var p3).Should().BeTrue();
        first.TryGetProperty("isEnabled", out var p4).Should().BeTrue();
        first.TryGetProperty("isMandatory", out var p5).Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreference_ValidCategory_Returns200WithUpdatedPreferences()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Disable a non-mandatory category (e.g., DailyDigest)
        var response = await client.PutAsJsonAsync("/api/users/me/email-preferences", new
        {
            Category = "DailyDigest",
            IsEnabled = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        var prefs = result.GetProperty("preferences");
        var dailyDigest = prefs.EnumerateArray()
            .First(p => p.GetProperty("category").GetString() == "DailyDigest");
        dailyDigest.GetProperty("isEnabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePreference_InvalidCategory_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/email-preferences", new
        {
            Category = "NonExistentCategory",
            IsEnabled = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePreference_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me/email-preferences", new
        {
            Category = "DailyDigest",
            IsEnabled = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePreference_DisableMandatoryCategory_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Security is a mandatory category - cannot be disabled
        var response = await client.PutAsJsonAsync("/api/users/me/email-preferences", new
        {
            Category = "Security",
            IsEnabled = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
