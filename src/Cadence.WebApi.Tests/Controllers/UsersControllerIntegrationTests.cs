using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for UsersController API endpoints.
/// Tests user management, profile, contact info, organization memberships, and org switching.
/// </summary>
[Collection("WebApi Integration")]
public class UsersControllerIntegrationTests
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

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task GET_Users_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_MyProfile_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PATCH_MyContact_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/users/me/contact", new
        {
            PhoneNumber = "+1234567890"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_MyOrganizations_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_SwitchOrganization_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/current-organization", new
        {
            OrganizationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Current User Profile
    // =========================================================================

    [Fact]
    public async Task GET_MyProfile_Authenticated_Returns200WithProfile()
    {
        var (factory, client, email) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        profile.GetProperty("email").GetString().Should().Be(email);
        profile.GetProperty("displayName").GetString().Should().Be("Test Admin");
        profile.GetProperty("systemRole").GetString().Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Get All Users (Admin/Manager)
    // =========================================================================

    [Fact]
    public async Task GET_Users_AsAdmin_Returns200WithPagination()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("users").GetArrayLength().Should().BeGreaterOrEqualTo(1);
        result.GetProperty("pagination").GetProperty("totalCount").GetInt32().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GET_Users_WithSearchFilter_Returns200()
    {
        var (factory, client, email) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/users?search={Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("users").GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GET_Users_WithPagination_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users?page=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("pagination").GetProperty("page").GetInt32().Should().Be(1);
        result.GetProperty("pagination").GetProperty("pageSize").GetInt32().Should().Be(5);
    }

    // =========================================================================
    // Update Profile (Admin)
    // =========================================================================

    [Fact]
    public async Task PUT_UpdateUser_ValidRequest_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get current user ID from profile
        var profileResponse = await client.GetAsync("/api/users/me");
        var profileJson = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<JsonElement>(profileJson, JsonOptions);
        var userId = profile.GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/users/{userId}", new
        {
            DisplayName = "Updated Admin Name"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        updated.GetProperty("displayName").GetString().Should().Be("Updated Admin Name");
    }

    [Fact]
    public async Task PUT_UpdateUser_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}", new
        {
            DisplayName = "Ghost User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update Contact Info
    // =========================================================================

    [Fact]
    public async Task PATCH_MyContact_ValidPhoneNumber_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PatchAsJsonAsync("/api/users/me/contact", new
        {
            PhoneNumber = "+1-555-123-4567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var contact = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        contact.GetProperty("phoneNumber").GetString().Should().Be("+1-555-123-4567");
    }

    [Fact]
    public async Task PATCH_MyContact_ClearPhoneNumber_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Set a phone number first
        await client.PatchAsJsonAsync("/api/users/me/contact", new
        {
            PhoneNumber = "+1-555-999-0000"
        });

        // Clear it
        var response = await client.PatchAsJsonAsync("/api/users/me/contact", new
        {
            PhoneNumber = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Get My Organizations
    // =========================================================================

    [Fact]
    public async Task GET_MyOrganizations_Authenticated_Returns200WithMemberships()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/users/me/organizations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("memberships").GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // Switch Organization
    // =========================================================================

    [Fact]
    public async Task POST_SwitchOrganization_ValidOrg_Returns200WithNewToken()
    {
        var (factory, client, email) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get the org we created during setup
        var orgsResponse = await client.GetAsync("/api/users/me/organizations");
        var orgsJson = await orgsResponse.Content.ReadAsStringAsync();
        var orgs = JsonSerializer.Deserialize<JsonElement>(orgsJson, JsonOptions);
        var firstMembership = orgs.GetProperty("memberships")[0];
        var orgId = firstMembership.GetProperty("organizationId").GetString();

        var response = await client.PostAsJsonAsync("/api/users/current-organization", new
        {
            OrganizationId = Guid.Parse(orgId!)
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("newToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("organizationName").GetString().Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Get User By ID (Admin)
    // =========================================================================

    [Fact]
    public async Task GET_UserById_ExistingUser_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get current user ID
        var profileResponse = await client.GetAsync("/api/users/me");
        var profileJson = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<JsonElement>(profileJson, JsonOptions);
        var userId = profile.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/users/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        user.GetProperty("id").GetString().Should().Be(userId);
    }

    [Fact]
    public async Task GET_UserById_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Get User Exercise Assignments
    // =========================================================================

    [Fact]
    public async Task GET_UserExerciseAssignments_OwnAssignments_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get current user ID
        var profileResponse = await client.GetAsync("/api/users/me");
        var profileJson = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<JsonElement>(profileJson, JsonOptions);
        var userId = profile.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/users/{userId}/exercise-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_UserExerciseAssignments_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}/exercise-assignments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get User Memberships
    // =========================================================================

    [Fact]
    public async Task GET_UserMemberships_OwnMemberships_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get current user ID
        var profileResponse = await client.GetAsync("/api/users/me");
        var profileJson = await profileResponse.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<JsonElement>(profileJson, JsonOptions);
        var userId = profile.GetProperty("id").GetString();

        var response = await client.GetAsync($"/api/users/{userId}/memberships");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var memberships = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        memberships.GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GET_UserMemberships_NonExistentUser_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}/memberships");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Change Role (Admin)
    // =========================================================================

    [Fact]
    public async Task PATCH_ChangeRole_NonExistentUser_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PatchAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", new
        {
            SystemRole = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Deactivate / Reactivate (Admin)
    // =========================================================================

    [Fact]
    public async Task POST_DeactivateUser_NonExistentUser_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync($"/api/users/{Guid.NewGuid()}/deactivate", new
        {
            Reason = "Test deactivation"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_ReactivateUser_NonExistentUser_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsync($"/api/users/{Guid.NewGuid()}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
