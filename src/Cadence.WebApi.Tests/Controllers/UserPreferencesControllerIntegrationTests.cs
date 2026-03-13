using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for UserPreferencesController API endpoints.
/// Tests get, update, reset, and validation of user display preferences.
/// </summary>
[Collection("WebApi Integration")]
public class UserPreferencesControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a fresh factory + authenticated admin client with org context.
    /// Returns disposable factory, client, and admin email.
    /// </summary>
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

    // =========================================================================
    // Auth / Smoke Tests
    // =========================================================================

    [Fact]
    public async Task GET_Preferences_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_Preferences_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "Dark"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DELETE_Preferences_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/users/me/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Preferences Tests
    // =========================================================================

    [Fact]
    public async Task GET_Preferences_Authenticated_Returns200WithDefaults()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users/me/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        preferences.GetProperty("theme").GetString().Should().Be("System");
        preferences.GetProperty("displayDensity").GetString().Should().Be("Comfortable");
        preferences.GetProperty("timeFormat").GetString().Should().Be("TwentyFourHour");
    }

    // =========================================================================
    // Update Preferences Tests
    // =========================================================================

    [Fact]
    public async Task PUT_Preferences_ValidTheme_Returns200WithUpdatedValue()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "Dark"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        preferences.GetProperty("theme").GetString().Should().Be("Dark");
    }

    [Fact]
    public async Task PUT_Preferences_ValidDensity_Returns200WithUpdatedValue()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            DisplayDensity = "Compact"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        preferences.GetProperty("displayDensity").GetString().Should().Be("Compact");
    }

    [Fact]
    public async Task PUT_Preferences_ValidTimeFormat_Returns200WithUpdatedValue()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            TimeFormat = "TwelveHour"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        preferences.GetProperty("timeFormat").GetString().Should().Be("TwelveHour");
    }

    [Fact]
    public async Task PUT_Preferences_AllFields_Returns200WithAllUpdated()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "Light",
            DisplayDensity = "Compact",
            TimeFormat = "TwelveHour"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        preferences.GetProperty("theme").GetString().Should().Be("Light");
        preferences.GetProperty("displayDensity").GetString().Should().Be("Compact");
        preferences.GetProperty("timeFormat").GetString().Should().Be("TwelveHour");
    }

    [Fact]
    public async Task PUT_Preferences_InvalidTheme_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "InvalidTheme"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_Preferences_InvalidDensity_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            DisplayDensity = "InvalidDensity"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_Preferences_InvalidTimeFormat_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            TimeFormat = "InvalidFormat"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Reset Preferences Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Preferences_Authenticated_Returns200WithDefaults()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // First update to non-default values
        await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "Dark",
            DisplayDensity = "Compact",
            TimeFormat = "TwelveHour"
        });

        // Reset
        var response = await client.DeleteAsync("/api/users/me/preferences");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        preferences.GetProperty("theme").GetString().Should().Be("System");
        preferences.GetProperty("displayDensity").GetString().Should().Be("Comfortable");
        preferences.GetProperty("timeFormat").GetString().Should().Be("TwentyFourHour");
    }

    // =========================================================================
    // End-to-End: Get defaults -> Update -> Verify -> Reset -> Verify defaults
    // =========================================================================

    [Fact]
    public async Task EndToEnd_GetUpdateResetPreferences_WorksCorrectly()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get defaults
        var getResponse = await client.GetAsync("/api/users/me/preferences");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var defaultJson = await getResponse.Content.ReadAsStringAsync();
        var defaults = JsonSerializer.Deserialize<JsonElement>(defaultJson, JsonOptions);
        defaults.GetProperty("theme").GetString().Should().Be("System");

        // Update all
        var updateResponse = await client.PutAsJsonAsync("/api/users/me/preferences", new
        {
            Theme = "Dark",
            DisplayDensity = "Compact",
            TimeFormat = "TwelveHour"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update persisted
        var verifyResponse = await client.GetAsync("/api/users/me/preferences");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyJson = await verifyResponse.Content.ReadAsStringAsync();
        var verified = JsonSerializer.Deserialize<JsonElement>(verifyJson, JsonOptions);
        verified.GetProperty("theme").GetString().Should().Be("Dark");
        verified.GetProperty("displayDensity").GetString().Should().Be("Compact");
        verified.GetProperty("timeFormat").GetString().Should().Be("TwelveHour");

        // Reset to defaults
        var resetResponse = await client.DeleteAsync("/api/users/me/preferences");
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify reset
        var finalResponse = await client.GetAsync("/api/users/me/preferences");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalJson = await finalResponse.Content.ReadAsStringAsync();
        var final = JsonSerializer.Deserialize<JsonElement>(finalJson, JsonOptions);
        final.GetProperty("theme").GetString().Should().Be("System");
        final.GetProperty("displayDensity").GetString().Should().Be("Comfortable");
        final.GetProperty("timeFormat").GetString().Should().Be("TwentyFourHour");
    }
}
