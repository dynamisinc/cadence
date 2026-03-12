using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for SystemSettingsController API endpoints.
/// Tests admin-only access, get settings, and update settings.
/// </summary>
[Collection("WebApi Integration")]
public class SystemSettingsControllerIntegrationTests
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
    public async Task GetSettings_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSettings_NonAdminUser_Returns403()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        // Register first user (admin)
        var adminEmail = $"admin-{Guid.NewGuid()}@example.com";
        var adminRegister = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(adminEmail, "Password123!", "Admin"));
        adminRegister.StatusCode.Should().Be(HttpStatusCode.Created);

        // Register second user (non-admin)
        var userEmail = $"user-{Guid.NewGuid()}@example.com";
        var userRegister = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(userEmail, "Password123!", "Regular User"));
        userRegister.StatusCode.Should().Be(HttpStatusCode.Created);

        var userAuth = await userRegister.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userAuth!.AccessToken);

        var response = await client.GetAsync("/api/system-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSettings_Admin_Returns200WithSettings()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/system-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var settings = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        settings.TryGetProperty("effectiveSupportAddress", out var p1).Should().BeTrue();
        settings.TryGetProperty("effectiveDefaultSenderAddress", out var p2).Should().BeTrue();
        settings.TryGetProperty("effectiveDefaultSenderName", out var p3).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSettings_Admin_Returns200WithUpdatedSettings()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync("/api/system-settings", new
        {
            SupportAddress = "newsupport@test.com",
            DefaultSenderName = "Updated Sender"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var settings = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        settings.GetProperty("supportAddress").GetString().Should().Be("newsupport@test.com");
        settings.GetProperty("defaultSenderName").GetString().Should().Be("Updated Sender");
    }

    [Fact]
    public async Task UpdateSettings_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/system-settings", new
        {
            SupportAddress = "test@test.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
