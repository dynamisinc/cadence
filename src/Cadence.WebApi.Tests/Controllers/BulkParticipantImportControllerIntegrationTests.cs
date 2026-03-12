using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Organizations.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace Cadence.WebApi.Tests.Controllers;

/// <summary>
/// Integration tests for BulkParticipantImportController API endpoints.
/// Tests file upload, template download, import history, and pending assignments.
/// </summary>
[Collection("WebApi Integration")]
public class BulkParticipantImportControllerIntegrationTests
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

    private static async Task<JsonElement> CreateExerciseAsync(HttpClient client, string? name = null)
    {
        var exerciseName = name ?? $"Test Exercise {Guid.NewGuid():N}"[..40];
        var response = await client.PostAsJsonAsync("/api/exercises", new
        {
            Name = exerciseName,
            ExerciseType = 0,
            ScheduledDate = "2026-06-15",
            Description = "Integration test exercise",
            TimeZoneId = "UTC"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
    }

    // =========================================================================
    // Authentication Tests
    // =========================================================================

    [Fact]
    public async Task POST_Upload_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var exerciseId = Guid.NewGuid();
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("test"), "file", "test.csv");

        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_ImportHistory_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/exercises/{Guid.NewGuid()}/participants/bulk-import/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_PendingAssignments_WithoutToken_Returns401()
    {
        var factory = new CadenceWebApplicationFactory();
        using var _ = factory;
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/exercises/{Guid.NewGuid()}/participants/bulk-import/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Template Download Tests (AllowAnonymous)
    // =========================================================================

    [Fact]
    public async Task GET_Template_WithoutToken_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        // Need an exercise for the route, but template is AllowAnonymous
        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Use anonymous client
        var anonClient = factory.CreateClient();
        var response = await anonClient.GetAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/template");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Contain("text/csv");
    }

    [Fact]
    public async Task GET_Template_CsvFormat_ReturnsFile()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var anonClient = factory.CreateClient();
        var response = await anonClient.GetAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/template?format=csv");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    // =========================================================================
    // Upload Tests
    // =========================================================================

    [Fact]
    public async Task POST_Upload_WithoutFile_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        // Post with empty multipart content (no file)
        var content = new MultipartFormDataContent();

        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Upload_EmptyFile_Returns400()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "empty.csv");

        var response = await client.PostAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Import History Tests
    // =========================================================================

    [Fact]
    public async Task GET_ImportHistory_ExistingExercise_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var records = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        records.ValueKind.Should().Be(JsonValueKind.Array);
        records.GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Pending Assignments Tests
    // =========================================================================

    [Fact]
    public async Task GET_PendingAssignments_ExistingExercise_Returns200()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var assignments = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        assignments.ValueKind.Should().Be(JsonValueKind.Array);
        assignments.GetArrayLength().Should().Be(0);
    }

    // =========================================================================
    // Import Record Tests
    // =========================================================================

    [Fact]
    public async Task GET_ImportRecord_NonExistent_Returns404()
    {
        var (factory, client, _) = await SetupAuthenticatedClientAsync();
        using var _ = factory;

        var exercise = await CreateExerciseAsync(client);
        var exerciseId = exercise.GetProperty("id").GetString();

        var response = await client.GetAsync(
            $"/api/exercises/{exerciseId}/participants/bulk-import/records/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
