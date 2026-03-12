using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for NotificationsController API endpoints.
/// Tests user notification retrieval, mark-as-read, mark-all-read, and unread count.
/// </summary>
[Collection("WebApi Integration")]
public class NotificationsControllerIntegrationTests
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
    public async Task GET_Notifications_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_UnreadCount_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_MarkAsRead_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_MarkAllAsRead_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Notifications Tests
    // =========================================================================

    [Fact]
    public async Task GET_Notifications_Authenticated_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Notifications_WithPagination_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/notifications?limit=5&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Notifications_WithLargeLimit_ClampedTo100_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Limit is clamped to 100 max by the controller
        var response = await client.GetAsync("/api/notifications?limit=500");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Unread Count Tests
    // =========================================================================

    [Fact]
    public async Task GET_UnreadCount_Authenticated_Returns200WithCount()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/notifications/unread-count");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("unreadCount").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    // =========================================================================
    // Mark As Read Tests
    // =========================================================================

    [Fact]
    public async Task POST_MarkAsRead_NonExistentNotification_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Mark All As Read Tests
    // =========================================================================

    [Fact]
    public async Task POST_MarkAllAsRead_Authenticated_Returns200WithCount()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsync("/api/notifications/read-all", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("markedCount").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    // =========================================================================
    // End-to-End: Get notifications, check unread, mark all read
    // =========================================================================

    [Fact]
    public async Task EndToEnd_GetNotificationsCheckUnreadMarkAllRead_WorksCorrectly()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Get notifications (should be empty or have system notifications)
        var getResponse = await client.GetAsync("/api/notifications");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check unread count
        var countResponse = await client.GetAsync("/api/notifications/unread-count");
        countResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var countJson = await countResponse.Content.ReadAsStringAsync();
        var countResult = JsonSerializer.Deserialize<JsonElement>(countJson, JsonOptions);
        var initialUnread = countResult.GetProperty("unreadCount").GetInt32();
        initialUnread.Should().BeGreaterOrEqualTo(0);

        // Mark all as read
        var markAllResponse = await client.PostAsync("/api/notifications/read-all", null);
        markAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify unread count is now 0
        var finalCountResponse = await client.GetAsync("/api/notifications/unread-count");
        finalCountResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalCountJson = await finalCountResponse.Content.ReadAsStringAsync();
        var finalCountResult = JsonSerializer.Deserialize<JsonElement>(finalCountJson, JsonOptions);
        finalCountResult.GetProperty("unreadCount").GetInt32().Should().Be(0);
    }
}
