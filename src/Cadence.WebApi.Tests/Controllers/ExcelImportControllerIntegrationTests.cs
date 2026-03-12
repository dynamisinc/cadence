using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for ExcelImportController API endpoints.
/// Tests file upload, session management, and import workflow.
/// </summary>
[Collection("WebApi Integration")]
public class ExcelImportControllerIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<(CadenceWebApplicationFactory Factory, HttpClient Client)>
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

        return (factory, client);
    }

    // =========================================================================
    // Auth Tests
    // =========================================================================

    [Fact]
    public async Task POST_Upload_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.xlsx");

        var response = await client.PostAsync("/api/import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Upload Tests
    // =========================================================================

    [Fact]
    public async Task POST_Upload_NoFile_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        using var content = new MultipartFormDataContent();
        // Send empty form

        var response = await client.PostAsync("/api/import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Upload_UnsupportedFormat_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.pdf");

        var response = await client.PostAsync("/api/import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Unsupported file format");
    }

    [Fact]
    public async Task POST_Upload_InvalidExcelFile_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Create a fake xlsx file (invalid content but correct extension)
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 0x00, 0x01, 0x02, 0x03 }), "file", "test.xlsx");

        var response = await client.PostAsync("/api/import/upload", content);

        // Should return 400 because file content is invalid
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Session Tests
    // =========================================================================

    [Fact]
    public async Task GET_Session_NonExistent_Returns404()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/import/sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_SessionMappings_NonExistentSession_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.GetAsync($"/api/import/sessions/{Guid.NewGuid()}/mappings");

        // Invalid session returns 400 (InvalidOperationException)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Cancel Import Tests
    // =========================================================================

    [Fact]
    public async Task DELETE_CancelImport_NonExistentSession_Returns204()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Cancel is idempotent - always returns 204
        var response = await client.DeleteAsync($"/api/import/sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // =========================================================================
    // Validate Import Tests
    // =========================================================================

    [Fact]
    public async Task POST_Validate_InvalidSession_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/import/validate", new
        {
            SessionId = Guid.NewGuid(),
            Mappings = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Execute Import Tests
    // =========================================================================

    [Fact]
    public async Task POST_Execute_InvalidSession_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/import/execute", new
        {
            SessionId = Guid.NewGuid(),
            ExerciseId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Select Worksheet Tests
    // =========================================================================

    [Fact]
    public async Task POST_SelectWorksheet_InvalidSession_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PostAsJsonAsync("/api/import/select-worksheet", new
        {
            SessionId = Guid.NewGuid(),
            WorksheetName = "Sheet1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Update Rows Tests
    // =========================================================================

    [Fact]
    public async Task PATCH_UpdateRows_InvalidSession_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var sessionId = Guid.NewGuid();
        var response = await client.PatchAsJsonAsync($"/api/import/sessions/{sessionId}/rows", new
        {
            SessionId = sessionId,
            Updates = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_UpdateRows_MismatchedSessionId_Returns400()
    {
        var (factory, client) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var response = await client.PatchAsJsonAsync($"/api/import/sessions/{Guid.NewGuid()}/rows", new
        {
            SessionId = Guid.NewGuid(), // Different from URL
            Updates = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
