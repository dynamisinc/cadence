using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for OrganizationSuggestionsController API endpoints.
/// Tests org-curated autocomplete suggestion management.
/// </summary>
[Collection("WebApi Integration")]
public class OrganizationSuggestionsControllerIntegrationTests
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
    public async Task GET_Suggestions_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/organizations/current/suggestions?fieldName=Track");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_CreateSuggestion_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/organizations/current/suggestions",
            new { FieldName = "Track", Value = "Test Track" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Get Suggestions Tests
    // =========================================================================

    [Fact]
    public async Task GET_Suggestions_ValidFieldName_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/suggestions?fieldName=Track");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var suggestions = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        suggestions.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GET_Suggestions_InvalidFieldName_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/suggestions?fieldName=InvalidField");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Source")]
    [InlineData("Target")]
    [InlineData("Track")]
    [InlineData("LocationName")]
    [InlineData("LocationType")]
    [InlineData("ResponsibleController")]
    public async Task GET_Suggestions_AllValidFieldNames_Returns200(string fieldName)
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/organizations/current/suggestions?fieldName={fieldName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =========================================================================
    // Create Suggestion Tests
    // =========================================================================

    [Fact]
    public async Task POST_CreateSuggestion_ValidRequest_Returns201()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/organizations/current/suggestions", new
        {
            FieldName = "Track",
            Value = $"Test Track {Guid.NewGuid():N}"[..30]
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var suggestion = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        suggestion.GetProperty("fieldName").GetString().Should().Be("Track");
        suggestion.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Bulk Create Tests
    // =========================================================================

    [Fact]
    public async Task POST_BulkCreateSuggestions_ValidRequest_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/organizations/current/suggestions/bulk", new
        {
            FieldName = "Source",
            Values = new[] { $"Source A {Guid.NewGuid():N}"[..20], $"Source B {Guid.NewGuid():N}"[..20] }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        result.GetProperty("created").GetInt32().Should().BeGreaterOrEqualTo(1);
    }

    // =========================================================================
    // Historical Values Tests
    // =========================================================================

    [Fact]
    public async Task GET_HistoricalValues_ValidFieldName_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/suggestions/historical?fieldName=Track");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var values = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        values.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GET_HistoricalValues_InvalidFieldName_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync("/api/organizations/current/suggestions/historical?fieldName=Bogus");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Block / Unblock Tests
    // =========================================================================

    [Fact]
    public async Task POST_BlockValue_ValidRequest_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/organizations/current/suggestions/block", new
        {
            FieldName = "Target",
            Value = $"Blocked Value {Guid.NewGuid():N}"[..30]
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var blocked = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        blocked.GetProperty("isBlocked").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DELETE_UnblockValue_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/organizations/current/suggestions/block/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete Suggestion Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_Suggestion_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.DeleteAsync($"/api/organizations/current/suggestions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Suggestion_ExistingSuggestion_Returns204()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create a suggestion first
        var createResponse = await client.PostAsJsonAsync("/api/organizations/current/suggestions", new
        {
            FieldName = "Track",
            Value = $"Deletable {Guid.NewGuid():N}"[..30]
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = JsonSerializer.Deserialize<JsonElement>(
            await createResponse.Content.ReadAsStringAsync(), JsonOptions);
        var id = created.GetProperty("id").GetString();

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/organizations/current/suggestions/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
