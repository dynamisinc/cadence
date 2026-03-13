using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for EulaController API endpoints.
/// Tests EULA status retrieval and acceptance.
/// </summary>
[Collection("WebApi Integration")]
public class EulaControllerIntegrationTests
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
    public async Task GetStatus_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/eula/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatus_Authenticated_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/eula/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        status.TryGetProperty("required", out var req).Should().BeTrue();
    }

    [Fact]
    public async Task Accept_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/eula/accept", new { Version = "1.0" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Accept_EmptyVersion_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/eula/accept", new { Version = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Accept_WithEulaConfigured_Returns204()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // First configure a EULA via system settings (admin)
        var settingsResponse = await client.PutAsJsonAsync("/api/system-settings", new
        {
            EulaContent = "# Test EULA\n\nPlease accept these terms.",
            EulaVersion = "1.0"
        });
        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify EULA is required
        var statusResponse = await client.GetAsync("/api/eula/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var statusJson = await statusResponse.Content.ReadAsStringAsync();
        var status = JsonSerializer.Deserialize<JsonElement>(statusJson, JsonOptions);
        status.GetProperty("required").GetBoolean().Should().BeTrue();
        status.GetProperty("version").GetString().Should().Be("1.0");

        // Accept the EULA
        var acceptResponse = await client.PostAsJsonAsync("/api/eula/accept", new { Version = "1.0" });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify no longer required
        var statusAfter = await client.GetAsync("/api/eula/status");
        var afterJson = await statusAfter.Content.ReadAsStringAsync();
        var afterStatus = JsonSerializer.Deserialize<JsonElement>(afterJson, JsonOptions);
        afterStatus.GetProperty("required").GetBoolean().Should().BeFalse();
    }
}
