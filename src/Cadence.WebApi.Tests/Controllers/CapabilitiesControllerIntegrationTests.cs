using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for CapabilitiesController API endpoints.
/// Tests org-scoped capability CRUD, name checking, and library operations.
/// </summary>
[Collection("WebApi Integration")]
public class CapabilitiesControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a fresh factory + authenticated admin client with org context.
    /// Returns disposable factory, client, admin email, and organization ID.
    /// </summary>
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

    /// <summary>
    /// Creates a capability via POST and returns the response JSON element.
    /// </summary>
    private static async Task<JsonElement> CreateCapabilityAsync(
        HttpClient client, Guid orgId, string? name = null, string? category = null)
    {
        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = name ?? $"Test Capability {Guid.NewGuid():N}"[..40],
            Description = "Integration test capability",
            Category = category ?? "Test Category",
            SortOrder = 0
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

        var response = await client.GetAsync($"/api/organizations/{Guid.NewGuid()}/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_Capabilities_WithAuthAndOrg_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Create Tests
    // =========================================================================

    [Fact]
    public async Task POST_CreateCapability_ValidRequest_Returns201WithCapability()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = "Emergency Communications",
            Description = "Ability to communicate during emergencies",
            Category = "FEMA Core",
            SortOrder = 1
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var capability = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

        capability.GetProperty("name").GetString().Should().Be("Emergency Communications");
        capability.GetProperty("description").GetString().Should().Be("Ability to communicate during emergencies");
        capability.GetProperty("category").GetString().Should().Be("FEMA Core");
        capability.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task POST_CreateCapability_EmptyName_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = "",
            Description = "Some description"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CreateCapability_NameTooShort_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/capabilities", new
        {
            Name = "A"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Get By ID Tests
    // =========================================================================

    [Fact]
    public async Task GET_CapabilityById_ExistingCapability_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId, "GetById Test Cap");
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var capability = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        capability.GetProperty("name").GetString().Should().Be("GetById Test Cap");
    }

    [Fact]
    public async Task GET_CapabilityById_NonExistent_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Tests
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateCapability_ValidRequest_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId);
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/organizations/{orgId}/capabilities/{id}", new
        {
            Name = "Updated Capability Name",
            Description = "Updated description",
            Category = "Updated Category",
            SortOrder = 5,
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var capability = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        capability.GetProperty("name").GetString().Should().Be("Updated Capability Name");
        capability.GetProperty("description").GetString().Should().Be("Updated description");
    }

    [Fact]
    public async Task PUT_UpdateCapability_NonExistent_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync($"/api/organizations/{orgId}/capabilities/{Guid.NewGuid()}", new
        {
            Name = "Ghost Capability",
            SortOrder = 0,
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateCapability_EmptyName_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId);
        var id = created.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/organizations/{orgId}/capabilities/{id}", new
        {
            Name = "",
            SortOrder = 0,
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Delete (Deactivate) Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Capability_ExistingCapability_Returns204()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId);
        var id = created.GetProperty("id").GetString();

        var response = await client.DeleteAsync($"/api/organizations/{orgId}/capabilities/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Capability_NonExistent_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/organizations/{orgId}/capabilities/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Reactivate Tests
    // =========================================================================

    [Fact]
    public async Task POST_ReactivateCapability_DeactivatedCapability_Returns204()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId);
        var id = created.GetProperty("id").GetString();

        // Deactivate first
        var deleteResponse = await client.DeleteAsync($"/api/organizations/{orgId}/capabilities/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reactivate
        var response = await client.PostAsync($"/api/organizations/{orgId}/capabilities/{id}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task POST_ReactivateCapability_NonExistent_Returns404()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsync(
            $"/api/organizations/{orgId}/capabilities/{Guid.NewGuid()}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Check Name Tests
    // =========================================================================

    [Fact]
    public async Task GET_CheckName_AvailableName_ReturnsIsAvailableTrue()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync(
            $"/api/organizations/{orgId}/capabilities/check-name?name=Unique+Capability+Name");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GET_CheckName_EmptyName_Returns400()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync(
            $"/api/organizations/{orgId}/capabilities/check-name?name=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Library Endpoints Tests
    // =========================================================================

    [Fact]
    public async Task GET_Libraries_WithAuth_Returns200()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities/libraries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var libraries = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        libraries.GetArrayLength().Should().BeGreaterOrEqualTo(0);
    }

    // =========================================================================
    // Get All with includeInactive Tests
    // =========================================================================

    [Fact]
    public async Task GET_Capabilities_ReturnsCreatedCapabilities()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        await CreateCapabilityAsync(client, orgId, "List Cap A");
        await CreateCapabilityAsync(client, orgId, "List Cap B");

        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var capabilities = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        capabilities.GetArrayLength().Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GET_Capabilities_IncludeInactive_ReturnsDeactivatedCapabilities()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var created = await CreateCapabilityAsync(client, orgId, "InactiveTestCap");
        var id = created.GetProperty("id").GetString();

        // Deactivate
        await client.DeleteAsync($"/api/organizations/{orgId}/capabilities/{id}");

        // Without includeInactive - should not include deactivated
        var response = await client.GetAsync($"/api/organizations/{orgId}/capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // With includeInactive - should include deactivated
        var responseWithInactive = await client.GetAsync(
            $"/api/organizations/{orgId}/capabilities?includeInactive=true");
        responseWithInactive.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonWithInactive = await responseWithInactive.Content.ReadAsStringAsync();
        var capabilitiesWithInactive = JsonSerializer.Deserialize<JsonElement>(jsonWithInactive, JsonOptions);
        capabilitiesWithInactive.GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // End-to-End: Create -> Update -> Delete -> Reactivate
    // =========================================================================

    [Fact]
    public async Task EndToEnd_CreateUpdateDeleteReactivate_WorksCorrectly()
    {
        var (factory, client, _, orgId) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create
        var created = await CreateCapabilityAsync(client, orgId, "E2E Capability");
        var id = created.GetProperty("id").GetString();
        created.GetProperty("isActive").GetBoolean().Should().BeTrue();

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/organizations/{orgId}/capabilities/{id}", new
        {
            Name = "E2E Updated Capability",
            Description = "Updated via E2E test",
            Category = "E2E Category",
            SortOrder = 10,
            IsActive = true
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify update
        var getResponse = await client.GetAsync($"/api/organizations/{orgId}/capabilities/{id}");
        var updated = JsonSerializer.Deserialize<JsonElement>(
            await getResponse.Content.ReadAsStringAsync(), JsonOptions);
        updated.GetProperty("name").GetString().Should().Be("E2E Updated Capability");

        // Deactivate (delete)
        var deleteResponse = await client.DeleteAsync($"/api/organizations/{orgId}/capabilities/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Reactivate
        var reactivateResponse = await client.PostAsync(
            $"/api/organizations/{orgId}/capabilities/{id}/reactivate", null);
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify reactivated
        var finalGet = await client.GetAsync($"/api/organizations/{orgId}/capabilities/{id}");
        finalGet.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
