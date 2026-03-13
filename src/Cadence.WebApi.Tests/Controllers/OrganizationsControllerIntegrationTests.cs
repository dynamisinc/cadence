using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for OrganizationsController API endpoints.
/// Tests current org retrieval, member management, and invitation endpoints.
/// </summary>
[Collection("WebApi Integration")]
public class OrganizationsControllerIntegrationTests
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
    // Authentication Tests
    // =========================================================================

    [Fact]
    public async Task GET_CurrentOrganization_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/organizations/current");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CurrentMembers_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/organizations/current/members");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateInvitation_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/organizations/current/invitations",
            new { Email = "test@example.com", Role = "OrgUser" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Current Organization Tests
    // =========================================================================

    [Fact]
    public async Task GET_CurrentOrganization_WithAuthAndOrg_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var org = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        org.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
        org.GetProperty("slug").GetString().Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Member Management Tests
    // =========================================================================

    [Fact]
    public async Task GET_CurrentMembers_WithAuthAndOrg_Returns200WithMembers()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var members = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        // The admin who created the org should be a member
        members.GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // Invitation Tests
    // =========================================================================

    [Fact]
    public async Task GET_CurrentInvitations_WithAuthAndOrg_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/invitations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var invitations = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        invitations.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task POST_CreateInvitation_ValidEmail_Returns201()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var inviteEmail = $"invite-{Guid.NewGuid()}@example.com";
        var response = await client.PostAsJsonAsync("/api/organizations/current/invitations",
            new CreateInvitationRequest(inviteEmail));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var invitation = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        invitation.GetProperty("email").GetString().Should().Be(inviteEmail);
    }

    // =========================================================================
    // Invitation Validation (AllowAnonymous)
    // =========================================================================

    [Fact]
    public async Task GET_ValidateInvitationCode_InvalidCode_Returns404()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/invitations/validate/invalid-code-xyz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Approval Permissions Tests
    // =========================================================================

    [Fact]
    public async Task GET_ApprovalPermissions_WithAuthAndOrg_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/settings/approval-permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
