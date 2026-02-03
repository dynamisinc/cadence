using System.Net;
using System.Net.Http.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for AdminOrganizationsController API endpoints.
/// Tests the full request/response cycle including DI resolution, authorization, and database operations.
/// These tests ensure that all services are properly registered in the DI container.
/// </summary>
public class AdminOrganizationsControllerIntegrationTests : IClassFixture<CadenceWebApplicationFactory>
{
    private readonly CadenceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminOrganizationsControllerIntegrationTests(CadenceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper method to register as first user (Admin) and get authenticated client.
    /// Returns factory so caller can dispose it properly.
    /// </summary>
    private async Task<(CadenceWebApplicationFactory Factory, HttpClient Client, string AccessToken, string AdminEmail)> GetAuthenticatedAdminClientAsync()
    {
        var factory = new CadenceWebApplicationFactory();
        var client = factory.CreateClient();

        var email = $"admin-{Guid.NewGuid()}@example.com";
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(email, "Password123!", "Test Admin"));

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Role.Should().Be("Admin"); // First user is Admin

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.AccessToken);

        return (factory, client, authResponse.AccessToken!, email);
    }

    /// <summary>
    /// Helper to create a unique slug for testing.
    /// </summary>
    private static string UniqueSlug() => $"test-org-{Guid.NewGuid():N}"[..30];

    // =========================================================================
    // DI Registration Tests - These catch missing service registrations
    // =========================================================================

    #region DI Registration Tests

    [Fact]
    public async Task GET_Organizations_WithAdminToken_DoesNotThrow500()
    {
        // This test verifies that all dependencies of AdminOrganizationsController are registered
        // A 500 error would indicate a DI resolution failure
        var (factory, client, _, _) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Act
        var response = await client.GetAsync("/api/admin/organizations");

        // Assert - should not be 500 (which would indicate DI failure)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "500 error indicates DI registration failure - check ServiceCollectionExtensions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_OrganizationMembers_WithAdminToken_DoesNotThrow500()
    {
        // This test specifically verifies IMembershipService is registered
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // First create an organization
        var createResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Test Org DI {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var org = await createResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Act - this endpoint uses IMembershipService
        var response = await client.GetAsync($"/api/admin/organizations/{org!.Id}/members");

        // Assert - should not be 500 (which would indicate IMembershipService DI failure)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "500 error indicates IMembershipService not registered in DI container");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    // =========================================================================
    // Organization CRUD Tests
    // =========================================================================

    #region Organization CRUD Tests

    [Fact]
    public async Task POST_CreateOrganization_ValidRequest_Returns201()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Arrange
        var request = new CreateOrganizationRequest(
            $"Test Organization {Guid.NewGuid():N}"[..40],
            UniqueSlug(),
            "Test Description",
            "test@example.com",
            adminEmail);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        org.Should().NotBeNull();
        org!.Name.Should().Be(request.Name);
        org.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GET_Organizations_ReturnsListWithCount()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create a test organization first
        await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"List Test Org {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        // Act
        var response = await client.GetAsync("/api/admin/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrganizationListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.TotalCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GET_OrganizationById_ExistingOrg_Returns200()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org first
        var createResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"GetById Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var createdOrg = await createResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Act
        var response = await client.GetAsync($"/api/admin/organizations/{createdOrg!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var org = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        org.Should().NotBeNull();
        org!.Id.Should().Be(createdOrg.Id);
    }

    [Fact]
    public async Task GET_OrganizationById_NonExistent_Returns404()
    {
        var (factory, client, _, _) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Act
        var response = await client.GetAsync($"/api/admin/organizations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    // =========================================================================
    // Member Management Tests
    // =========================================================================

    #region Member Management Tests

    [Fact]
    public async Task GET_OrganizationMembers_NewOrg_ReturnsFirstAdmin()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org with first admin
        var createResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Members Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Act
        var response = await client.GetAsync($"/api/admin/organizations/{org!.Id}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var members = await response.Content.ReadFromJsonAsync<List<OrgMemberDto>>();
        members.Should().NotBeNull();
        // First admin should be added to the org
        members.Should().Contain(m => m.Email == adminEmail);
    }

    [Fact]
    public async Task POST_AddMember_ValidEmail_Returns201()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Add Member Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Create another user to add
        var userEmail = $"member-{Guid.NewGuid()}@example.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(userEmail, "Password123!", "Test Member"));

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/admin/organizations/{org!.Id}/members",
            new { Email = userEmail, Role = "OrgUser" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<OrgMemberDto>();
        member.Should().NotBeNull();
        member!.Email.Should().Be(userEmail);
        member.Role.Should().Be("OrgUser");
    }

    [Fact]
    public async Task POST_AddMember_NonExistentUser_Returns404()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"NonExistent Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Act - try to add non-existent user
        var response = await client.PostAsJsonAsync(
            $"/api/admin/organizations/{org!.Id}/members",
            new { Email = "nonexistent@example.com", Role = "OrgUser" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateMemberRole_ValidRequest_Returns200()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Update Role Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Create and add a user
        var userEmail = $"role-update-{Guid.NewGuid()}@example.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(userEmail, "Password123!", "Role Update User"));

        var addResponse = await client.PostAsJsonAsync(
            $"/api/admin/organizations/{org!.Id}/members",
            new { Email = userEmail, Role = "OrgUser" });

        var member = await addResponse.Content.ReadFromJsonAsync<OrgMemberDto>();

        // Act - update role
        var response = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{org.Id}/members/{member!.MembershipId}",
            new { Role = "OrgManager" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<MembershipDto>();
        updated.Should().NotBeNull();
        updated!.Role.Should().Be("OrgManager");
    }

    [Fact]
    public async Task DELETE_RemoveMember_ValidRequest_Returns200()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Remove Member Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Create and add a user
        var userEmail = $"remove-{Guid.NewGuid()}@example.com";
        await client.PostAsJsonAsync("/api/auth/register",
            new RegistrationRequest(userEmail, "Password123!", "Remove User"));

        var addResponse = await client.PostAsJsonAsync(
            $"/api/admin/organizations/{org!.Id}/members",
            new { Email = userEmail, Role = "OrgUser" });

        var member = await addResponse.Content.ReadFromJsonAsync<OrgMemberDto>();

        // Act - remove member
        var response = await client.DeleteAsync(
            $"/api/admin/organizations/{org.Id}/members/{member!.MembershipId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    // =========================================================================
    // Authorization Tests
    // =========================================================================

    #region Authorization Tests

    [Fact]
    public async Task GET_Organizations_WithoutToken_Returns401()
    {
        // Arrange - client without auth
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_OrganizationMembers_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/admin/organizations/{Guid.NewGuid()}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // Approval Policy Tests
    // =========================================================================

    #region Approval Policy Tests

    [Fact]
    public async Task PUT_UpdateApprovalPolicy_ValidRequest_Returns200()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Approval Policy Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();
        org.Should().NotBeNull();
        org!.InjectApprovalPolicy.Should().Be("Optional"); // Default value

        // Act - update approval policy to Required
        var response = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{org.Id}/settings/approval-policy",
            new { InjectApprovalPolicy = "Required" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        updated.Should().NotBeNull();
        updated!.InjectApprovalPolicy.Should().Be("Required");
    }

    [Fact]
    public async Task PUT_UpdateApprovalPolicy_ToDisabled_Returns200()
    {
        var (factory, client, _, adminEmail) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Create org
        var createOrgResponse = await client.PostAsJsonAsync("/api/admin/organizations",
            new CreateOrganizationRequest($"Policy Disabled Test {Guid.NewGuid():N}"[..40], UniqueSlug(), null, null, adminEmail));

        var org = await createOrgResponse.Content.ReadFromJsonAsync<OrganizationDto>();

        // Act - update approval policy to Disabled
        var response = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{org!.Id}/settings/approval-policy",
            new { InjectApprovalPolicy = "Disabled" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<OrganizationDto>();
        updated.Should().NotBeNull();
        updated!.InjectApprovalPolicy.Should().Be("Disabled");
    }

    [Fact]
    public async Task PUT_UpdateApprovalPolicy_NonExistentOrg_Returns404()
    {
        var (factory, client, _, _) = await GetAuthenticatedAdminClientAsync();
        using var _ = factory;

        // Act - try to update policy for non-existent org
        var response = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{Guid.NewGuid()}/settings/approval-policy",
            new { InjectApprovalPolicy = "Required" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_UpdateApprovalPolicy_WithoutToken_Returns401()
    {
        // Arrange - client without auth
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/admin/organizations/{Guid.NewGuid()}/settings/approval-policy",
            new { InjectApprovalPolicy = "Required" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // Helper DTOs for deserialization
    // =========================================================================

    private record OrganizationListResponse(List<OrganizationDto> Items, int TotalCount);
}
