using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for DeliveryMethodsController API endpoints.
/// Tests authenticated access and delivery method CRUD operations.
/// </summary>
[Collection("WebApi Integration")]
public class DeliveryMethodsControllerIntegrationTests
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
    public async Task GetAll_NoAuth_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/delivery-methods");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_Authenticated_Returns200WithArray()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/delivery-methods");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var methods = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        methods.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllIncludingInactive_Admin_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/delivery-methods/all");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var methods = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        methods.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Create_Admin_Returns201WithCreatedMethod()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/delivery-methods", new
        {
            Name = $"Test Method {Guid.NewGuid():N}"[..30],
            Description = "A test delivery method",
            SortOrder = 99,
            IsOther = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var method = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        method.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        method.GetProperty("description").GetString().Should().Be("A test delivery method");
    }

    [Fact]
    public async Task GetById_ExistingMethod_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create a method first
        var createResponse = await client.PostAsJsonAsync("/api/delivery-methods", new
        {
            Name = $"Get Test {Guid.NewGuid():N}"[..30],
            Description = "For get by id test"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/delivery-methods/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/delivery-methods/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ExistingMethod_Returns204()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create a method first
        var createResponse = await client.PostAsJsonAsync("/api/delivery-methods", new
        {
            Name = $"Delete Test {Guid.NewGuid():N}"[..30],
            Description = "For delete test"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var id = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/delivery-methods/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/delivery-methods/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
